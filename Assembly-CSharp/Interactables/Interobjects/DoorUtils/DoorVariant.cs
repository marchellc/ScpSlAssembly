using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorButtons;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using MapGeneration.StaticHelpers;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public abstract class DoorVariant : NetworkBehaviour, IServerInteractable, IInteractable, IRoomConnector, IDoorPermissionRequester, IBlockStaticBatching
{
	[Flags]
	private enum CollisionsDisablingReasons : byte
	{
		DoorClosing = 1,
		Scp106 = 2
	}

	public static readonly HashSet<DoorVariant> AllDoors = new HashSet<DoorVariant>();

	public static readonly Dictionary<RoomIdentifier, HashSet<DoorVariant>> DoorsByRoom = new Dictionary<RoomIdentifier, HashSet<DoorVariant>>();

	[SyncVar]
	public bool TargetState;

	[SyncVar]
	public ushort ActiveLocks;

	[SyncVar]
	public byte DoorId;

	public bool CanSeeThrough;

	[NonSerialized]
	public string DoorName;

	public DoorPermissionsPolicy RequiredPermissions;

	public BoxCollider[] IgnoredColliders;

	private bool _prevState;

	private ushort _prevLock;

	private byte _existenceCooldown = byte.MaxValue;

	private ReferenceHub _triggerPlayer;

	private CollisionsDisablingReasons _collidersStatus;

	private bool _collidersActivationPending;

	private float _remainingDeniedCooldown;

	private static int _serverDoorIdClock;

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public RoomIdentifier[] Rooms { get; private set; }

	public bool IsVisibleThrough
	{
		get
		{
			if (!this.CanSeeThrough)
			{
				return this.GetExactState() > 0f;
			}
			return true;
		}
	}

	public bool RoomsAlreadyRegistered { get; private set; }

	public BoxCollider[] AllColliders { get; private set; }

	[field: SerializeField]
	public ButtonVariant[] Buttons { get; private set; }

	public bool IsMoving
	{
		get
		{
			float exactState = this.GetExactState();
			if (!(exactState > 0f) || this.TargetState)
			{
				if (exactState < 1f)
				{
					return this.TargetState;
				}
				return false;
			}
			return true;
		}
	}

	public DoorPermissionsPolicy PermissionsPolicy => this.RequiredPermissions;

	public string RequesterLogSignature
	{
		get
		{
			if (base.TryGetComponent<DoorNametagExtension>(out var component))
			{
				return "Door_" + component.GetName;
			}
			if (!this.RoomsAlreadyRegistered || this.Rooms.Length == 0)
			{
				return "Door_UNKNOWN";
			}
			return "Door_" + this.Rooms[0].Name;
		}
	}

	protected float DeniedCooldown => 1f;

	public bool NetworkTargetState
	{
		get
		{
			return this.TargetState;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.TargetState, 1uL, null);
		}
	}

	public ushort NetworkActiveLocks
	{
		get
		{
			return this.ActiveLocks;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ActiveLocks, 2uL, null);
		}
	}

	public byte NetworkDoorId
	{
		get
		{
			return this.DoorId;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.DoorId, 4uL, null);
		}
	}

	public static event Action<DoorVariant> OnInstanceCreated;

	public static event Action<DoorVariant> OnInstanceRemoved;

	public event Action OnRoomsRegistered;

	public event Action OnStateChanged;

	public event Action OnLockChanged;

	[Server]
	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void Interactables.Interobjects.DoorUtils.DoorVariant::ServerInteract(ReferenceHub,System.Byte)' called when server was not active");
			return;
		}
		bool canOpen = this.RequiredPermissions.CheckPermissions(ply, this, out var callback);
		bool pluginRequestSent = false;
		if (this.ActiveLocks > 0 && !this.TryResolveLock(ply, out pluginRequestSent))
		{
			if (this._remainingDeniedCooldown <= 0f)
			{
				this._remainingDeniedCooldown = this.DeniedCooldown;
				this.LockBypassDenied(ply, colliderId);
				callback?.Invoke(this, success: false);
			}
			PlayerEvents.OnInteractedDoor(new PlayerInteractedDoorEventArgs(ply, this, canOpen: false));
		}
		else
		{
			if (!this.AllowInteracting(ply, colliderId))
			{
				return;
			}
			if (!pluginRequestSent)
			{
				PlayerInteractingDoorEventArgs e = new PlayerInteractingDoorEventArgs(ply, this, canOpen);
				PlayerEvents.OnInteractingDoor(e);
				if (!e.IsAllowed)
				{
					return;
				}
				canOpen = e.CanOpen;
			}
			else
			{
				canOpen = true;
			}
			if (canOpen)
			{
				this.NetworkTargetState = !this.TargetState;
				this._triggerPlayer = ply;
				if (this.DoorName != null)
				{
					ServerLogs.AddLog(ServerLogs.Modules.Door, ply.LoggedNameFromRefHub() + " " + (this.TargetState ? "opened" : "closed") + " " + this.DoorName + ".", ServerLogs.ServerLogType.GameEvent);
				}
				callback?.Invoke(this, success: true);
			}
			else if (this._remainingDeniedCooldown <= 0f)
			{
				this._remainingDeniedCooldown = this.DeniedCooldown;
				this.PermissionsDenied(ply, colliderId);
				DoorEvents.TriggerAction(this, DoorAction.AccessDenied, ply);
				callback?.Invoke(this, success: false);
			}
			PlayerEvents.OnInteractedDoor(new PlayerInteractedDoorEventArgs(ply, this, canOpen));
		}
	}

	[Server]
	public void ServerChangeLock(DoorLockReason reason, bool newState)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void Interactables.Interobjects.DoorUtils.DoorVariant::ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason,System.Boolean)' called when server was not active");
			return;
		}
		DoorLockReason activeLocks = (DoorLockReason)this.ActiveLocks;
		activeLocks = ((!newState) ? ((DoorLockReason)((uint)activeLocks & (uint)(ushort)(~(int)reason))) : (activeLocks | reason));
		if ((uint)this.ActiveLocks != (uint)activeLocks)
		{
			if (newState)
			{
				if (this.ActiveLocks == 0)
				{
					DoorEvents.TriggerAction(this, DoorAction.Locked, null);
				}
			}
			else if (this.ActiveLocks != 0)
			{
				DoorEvents.TriggerAction(this, DoorAction.Unlocked, null);
			}
		}
		this.NetworkActiveLocks = (ushort)activeLocks;
	}

	public abstract void LockBypassDenied(ReferenceHub ply, byte colliderId);

	public abstract bool AnticheatPassageApproved();

	public abstract void PermissionsDenied(ReferenceHub ply, byte colliderId);

	public abstract bool AllowInteracting(ReferenceHub ply, byte colliderId);

	public abstract float GetExactState();

	public abstract bool IsConsideredOpen();

	protected virtual void LockChanged(ushort prevValue)
	{
	}

	internal virtual void TargetStateChanged()
	{
	}

	protected virtual void Awake()
	{
		this.AllColliders = this.GetColliders();
		ButtonVariant[] buttons = this.Buttons;
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].Init(this);
		}
		if (NetworkServer.active)
		{
			DoorVariant._serverDoorIdClock++;
			if (DoorVariant._serverDoorIdClock > 255)
			{
				DoorVariant._serverDoorIdClock = 1;
			}
			this.NetworkDoorId = (byte)DoorVariant._serverDoorIdClock;
		}
		DoorVariant.AllDoors.Add(this);
		if (SeedSynchronizer.MapGenerated)
		{
			this.RegisterRooms();
		}
		DoorVariant.OnInstanceCreated?.Invoke(this);
	}

	protected virtual void OnDestroy()
	{
		DoorVariant.AllDoors.Remove(this);
		if (this.Rooms == null)
		{
			return;
		}
		RoomIdentifier[] rooms = this.Rooms;
		foreach (RoomIdentifier key in rooms)
		{
			if (DoorVariant.DoorsByRoom.TryGetValue(key, out var value))
			{
				value.Remove(this);
			}
		}
		DoorVariant.OnInstanceRemoved?.Invoke(this);
	}

	protected virtual void Update()
	{
		if (this._existenceCooldown == 0)
		{
			if (this._prevLock != this.ActiveLocks)
			{
				this.LockChanged(this._prevLock);
				this.OnLockChanged?.Invoke();
				this._prevLock = this.ActiveLocks;
			}
			if (this._prevState != this.TargetState)
			{
				this.TargetStateChanged();
				this.OnStateChanged?.Invoke();
				DoorEvents.TriggerAction(this, (!this.TargetState) ? DoorAction.Closed : DoorAction.Opened, this._triggerPlayer);
				this._triggerPlayer = null;
				if (NetworkServer.active)
				{
					if (this.TargetState)
					{
						this._collidersStatus &= ~CollisionsDisablingReasons.DoorClosing;
					}
					else
					{
						this._collidersStatus |= CollisionsDisablingReasons.DoorClosing;
						this._collidersActivationPending = true;
					}
					this.SetColliders();
				}
				this._prevState = this.TargetState;
			}
		}
		else
		{
			this._existenceCooldown--;
		}
		if (NetworkServer.active && this._remainingDeniedCooldown > 0f)
		{
			this._remainingDeniedCooldown -= Time.deltaTime;
		}
		if (this._collidersActivationPending && !this.AnticheatPassageApproved())
		{
			this._collidersActivationPending = false;
			this._collidersStatus &= ~CollisionsDisablingReasons.DoorClosing;
			this.SetColliders();
		}
	}

	private bool TryResolveLock(ReferenceHub ply, out bool pluginRequestSent)
	{
		pluginRequestSent = false;
		if (ply.serverRoles.BypassMode)
		{
			return true;
		}
		DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)this.ActiveLocks);
		if (mode.HasFlagFast(DoorLockMode.CanClose) && this.TargetState)
		{
			return true;
		}
		if (mode.HasFlagFast(DoorLockMode.CanOpen) && !this.TargetState)
		{
			return true;
		}
		if (mode.HasFlagFast(DoorLockMode.ScpOverride) && ply.IsSCP())
		{
			return true;
		}
		PlayerInteractingDoorEventArgs e = new PlayerInteractingDoorEventArgs(ply, this, canOpen: false);
		PlayerEvents.OnInteractingDoor(e);
		pluginRequestSent = true;
		if (!e.IsAllowed)
		{
			return false;
		}
		if (e.CanOpen)
		{
			return true;
		}
		return false;
	}

	private BoxCollider[] GetColliders()
	{
		List<BoxCollider> list = ListPool<BoxCollider>.Shared.Rent();
		base.GetComponentsInChildren(list);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Collider collider = list[num];
			Collider[] ignoredColliders = this.IgnoredColliders;
			if (ignoredColliders.Contains(collider) || collider.TryGetComponent<ButtonVariant>(out var _))
			{
				list.RemoveAt(num);
			}
		}
		BoxCollider[] result = list.ToArray();
		ListPool<BoxCollider>.Shared.Return(list);
		return result;
	}

	private void SetColliders()
	{
		BoxCollider[] allColliders = this.AllColliders;
		for (int i = 0; i < allColliders.Length; i++)
		{
			allColliders[i].isTrigger = this._collidersStatus != (CollisionsDisablingReasons)0;
		}
	}

	private void RegisterRooms()
	{
		Vector3 position = base.transform.position;
		int num = 0;
		for (int i = 0; i < IRoomConnector.WorldDirections.Length; i++)
		{
			if ((position + IRoomConnector.WorldDirections[i]).TryGetRoom(out var room) && DoorVariant.DoorsByRoom.GetOrAddNew(room).Add(this))
			{
				IRoomConnector.RoomsNonAlloc[num] = room;
				num++;
			}
		}
		this.Rooms = new RoomIdentifier[num];
		Array.Copy(IRoomConnector.RoomsNonAlloc, this.Rooms, num);
		this.OnRoomsRegistered?.Invoke();
		this.RoomsAlreadyRegistered = true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationStage += OnMapStage;
		RoomIdentifier.OnRemoved += delegate(RoomIdentifier x)
		{
			DoorVariant.DoorsByRoom.Remove(x);
		};
	}

	private static void OnMapStage(MapGenerationPhase stage)
	{
		if (stage != MapGenerationPhase.ParentRoomRegistration)
		{
			return;
		}
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			allDoor.RegisterRooms();
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.TargetState);
			writer.WriteUShort(this.ActiveLocks);
			NetworkWriterExtensions.WriteByte(writer, this.DoorId);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.TargetState);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteUShort(this.ActiveLocks);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this.DoorId);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.TargetState, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this.ActiveLocks, null, reader.ReadUShort());
			base.GeneratedSyncVarDeserialize(ref this.DoorId, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.TargetState, null, reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ActiveLocks, null, reader.ReadUShort());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.DoorId, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
