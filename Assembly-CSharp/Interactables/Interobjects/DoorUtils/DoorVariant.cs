using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public abstract class DoorVariant : NetworkBehaviour, IServerInteractable, IInteractable, IRoomConnector
	{
		public static event Action<DoorVariant> OnInstanceCreated;

		public static event Action<DoorVariant> OnInstanceRemoved;

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public RoomIdentifier[] Rooms { get; private set; }

		public event Action OnRoomsRegistered;

		public bool IsVisibleThrough
		{
			get
			{
				return this.CanSeeThrough || this.GetExactState() > 0f;
			}
		}

		public bool RoomsAlreadyRegistered { get; private set; }

		public bool IsMoving
		{
			get
			{
				float exactState = this.GetExactState();
				return (exactState > 0f && !this.TargetState) || (exactState < 1f && this.TargetState);
			}
		}

		[Server]
		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Interactables.Interobjects.DoorUtils.DoorVariant::ServerInteract(ReferenceHub,System.Byte)' called when server was not active");
				return;
			}
			bool flag = false;
			bool flag2 = ply.GetRoleId() == RoleTypeId.Scp079 || this.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);
			if (this.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
			{
				DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)this.ActiveLocks);
				if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock || (this.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!this.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
				{
					flag2 = false;
					PlayerInteractingDoorEventArgs playerInteractingDoorEventArgs = new PlayerInteractingDoorEventArgs(ply, this, flag2);
					PlayerEvents.OnInteractingDoor(playerInteractingDoorEventArgs);
					flag = true;
					if (!playerInteractingDoorEventArgs.IsAllowed)
					{
						return;
					}
					flag2 = playerInteractingDoorEventArgs.CanOpen;
					if (!flag2)
					{
						this.LockBypassDenied(ply, colliderId);
						PlayerEvents.OnInteractedDoor(new PlayerInteractedDoorEventArgs(ply, this, flag2));
						return;
					}
				}
			}
			if (!this.AllowInteracting(ply, colliderId))
			{
				return;
			}
			if (!flag)
			{
				PlayerInteractingDoorEventArgs playerInteractingDoorEventArgs2 = new PlayerInteractingDoorEventArgs(ply, this, flag2);
				PlayerEvents.OnInteractingDoor(playerInteractingDoorEventArgs2);
				if (!playerInteractingDoorEventArgs2.IsAllowed)
				{
					return;
				}
				flag2 = playerInteractingDoorEventArgs2.CanOpen;
			}
			if (flag2)
			{
				this.NetworkTargetState = !this.TargetState;
				this._triggerPlayer = ply;
				if (this.DoorName != null)
				{
					ServerLogs.AddLog(ServerLogs.Modules.Door, string.Concat(new string[]
					{
						ply.LoggedNameFromRefHub(),
						" ",
						this.TargetState ? "opened" : "closed",
						" ",
						this.DoorName,
						"."
					}), ServerLogs.ServerLogType.GameEvent, false);
				}
			}
			else
			{
				this.PermissionsDenied(ply, colliderId);
				DoorEvents.TriggerAction(this, DoorAction.AccessDenied, ply);
			}
			PlayerEvents.OnInteractedDoor(new PlayerInteractedDoorEventArgs(ply, this, flag2));
		}

		[Server]
		public void ServerChangeLock(DoorLockReason reason, bool newState)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Interactables.Interobjects.DoorUtils.DoorVariant::ServerChangeLock(Interactables.Interobjects.DoorUtils.DoorLockReason,System.Boolean)' called when server was not active");
				return;
			}
			DoorLockReason doorLockReason = (DoorLockReason)this.ActiveLocks;
			if (newState)
			{
				doorLockReason |= reason;
			}
			else
			{
				doorLockReason &= ~reason;
			}
			if (this.ActiveLocks != (ushort)doorLockReason)
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
			this.NetworkActiveLocks = (ushort)doorLockReason;
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
			if (NetworkServer.active)
			{
				this._colliders = base.GetComponentsInChildren<BoxCollider>();
				if (this.IgnoredColliders != null)
				{
					this._colliders = this._colliders.Except(this.IgnoredColliders).ToArray<BoxCollider>();
				}
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
			Action<DoorVariant> onInstanceCreated = DoorVariant.OnInstanceCreated;
			if (onInstanceCreated == null)
			{
				return;
			}
			onInstanceCreated(this);
		}

		protected virtual void OnDestroy()
		{
			DoorVariant.AllDoors.Remove(this);
			if (this.Rooms == null)
			{
				return;
			}
			foreach (RoomIdentifier roomIdentifier in this.Rooms)
			{
				HashSet<DoorVariant> hashSet;
				if (DoorVariant.DoorsByRoom.TryGetValue(roomIdentifier, out hashSet))
				{
					hashSet.Remove(this);
				}
			}
			Action<DoorVariant> onInstanceRemoved = DoorVariant.OnInstanceRemoved;
			if (onInstanceRemoved == null)
			{
				return;
			}
			onInstanceRemoved(this);
		}

		protected virtual void Update()
		{
			if (this._existenceCooldown == 0)
			{
				if (this._prevLock != this.ActiveLocks)
				{
					this.LockChanged(this._prevLock);
					this._prevLock = this.ActiveLocks;
				}
				if (this._prevState != this.TargetState)
				{
					this.TargetStateChanged();
					DoorEvents.TriggerAction(this, this.TargetState ? DoorAction.Opened : DoorAction.Closed, this._triggerPlayer);
					this._triggerPlayer = null;
					if (NetworkServer.active)
					{
						if (this.TargetState)
						{
							this._collidersStatus &= ~DoorVariant.CollisionsDisablingReasons.DoorClosing;
						}
						else
						{
							this._collidersStatus |= DoorVariant.CollisionsDisablingReasons.DoorClosing;
							this._collidersActivationPending = true;
						}
						this.SetColliders();
					}
					this._prevState = this.TargetState;
				}
			}
			else
			{
				this._existenceCooldown -= 1;
			}
			if (this._collidersActivationPending && !this.AnticheatPassageApproved())
			{
				this._collidersActivationPending = false;
				this._collidersStatus &= ~DoorVariant.CollisionsDisablingReasons.DoorClosing;
				this.SetColliders();
			}
		}

		private void SetColliders()
		{
			BoxCollider[] colliders = this._colliders;
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].isTrigger = this._collidersStatus > (DoorVariant.CollisionsDisablingReasons)0;
			}
		}

		private void RegisterRooms()
		{
			Vector3 position = base.transform.position;
			int num = 0;
			for (int i = 0; i < IRoomConnector.WorldDirections.Length; i++)
			{
				Vector3Int vector3Int = RoomUtils.PositionToCoords(position + IRoomConnector.WorldDirections[i]);
				RoomIdentifier roomIdentifier;
				if (RoomIdentifier.RoomsByCoordinates.TryGetValue(vector3Int, out roomIdentifier))
				{
					if (DoorVariant.DoorsByRoom.GetOrAdd(roomIdentifier, () => new HashSet<DoorVariant>()).Add(this))
					{
						IRoomConnector.RoomsNonAlloc[num] = roomIdentifier;
						num++;
					}
				}
			}
			this.Rooms = new RoomIdentifier[num];
			Array.Copy(IRoomConnector.RoomsNonAlloc, this.Rooms, num);
			Action onRoomsRegistered = this.OnRoomsRegistered;
			if (onRoomsRegistered != null)
			{
				onRoomsRegistered();
			}
			this.RoomsAlreadyRegistered = true;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationStage += DoorVariant.OnMapStage;
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
			foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
			{
				doorVariant.RegisterRooms();
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool NetworkTargetState
		{
			get
			{
				return this.TargetState;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this.TargetState, 1UL, null);
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
				base.GeneratedSyncVarSetter<ushort>(value, ref this.ActiveLocks, 2UL, null);
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
				base.GeneratedSyncVarSetter<byte>(value, ref this.DoorId, 4UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteBool(this.TargetState);
				writer.WriteUShort(this.ActiveLocks);
				writer.WriteByte(this.DoorId);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteBool(this.TargetState);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteUShort(this.ActiveLocks);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteByte(this.DoorId);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.TargetState, null, reader.ReadBool());
				base.GeneratedSyncVarDeserialize<ushort>(ref this.ActiveLocks, null, reader.ReadUShort());
				base.GeneratedSyncVarDeserialize<byte>(ref this.DoorId, null, reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.TargetState, null, reader.ReadBool());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<ushort>(ref this.ActiveLocks, null, reader.ReadUShort());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.DoorId, null, reader.ReadByte());
			}
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

		public DoorPermissions RequiredPermissions;

		public BoxCollider[] IgnoredColliders;

		[NonSerialized]
		public string DoorName;

		private bool _prevState;

		private ushort _prevLock;

		private byte _existenceCooldown = byte.MaxValue;

		private ReferenceHub _triggerPlayer;

		private DoorVariant.CollisionsDisablingReasons _collidersStatus;

		private BoxCollider[] _colliders;

		private bool _collidersActivationPending;

		private static int _serverDoorIdClock;

		[Flags]
		private enum CollisionsDisablingReasons : byte
		{
			DoorClosing = 1,
			Scp106 = 2
		}
	}
}
