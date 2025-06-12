using System;
using System.Collections.Generic;
using System.Text;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079LockdownRoomAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier, IScp079AuxRegenModifier
{
	private enum ValidationError
	{
		None = 0,
		Unknown = 1,
		NotEnoughAux = 6,
		TierTooLow = 8,
		Cooldown = 31,
		NoDoors = 32
	}

	[SerializeField]
	private int _minimalTierIndex;

	[SerializeField]
	private float[] _regenerationPerTier;

	[SerializeField]
	private float _lockdownDuration;

	[SerializeField]
	private float _cooldown;

	[SerializeField]
	private int _cost;

	[SerializeField]
	private float _minStateToClose;

	[SerializeField]
	private AudioClip _lockdownStartSound;

	[SerializeField]
	private AudioClip _lockdownEndSound;

	private string _nameFormat;

	private string _failMessage;

	private string _unlockText;

	private double _nextUseTime;

	private bool _hasFailMessage;

	private bool _lockdownInEffect;

	private Scp079DoorLockChanger _doorLockChanger;

	private readonly HashSet<DoorVariant> _roomDoors = new HashSet<DoorVariant>();

	private readonly HashSet<DoorVariant> _doorsToLockDown = new HashSet<DoorVariant>();

	private readonly HashSet<DoorVariant> _alreadyLockedDown = new HashSet<DoorVariant>();

	private RoomIdentifier _lastLockedRoom;

	public override ActionName ActivationKey => ActionName.Scp079Lockdown;

	public override bool IsReady => this.ErrorCode == Scp079HudTranslation.Zoom;

	public override bool IsVisible
	{
		get
		{
			if (!Scp079CursorManager.LockCameras)
			{
				return this.ErrorCode != Scp079HudTranslation.HigherTierRequired;
			}
			return false;
		}
	}

	public override string AbilityName => string.Format(this._nameFormat, this._cost);

	public override bool DummyEmulationSupport => true;

	public override string FailMessage
	{
		get
		{
			if (!this._hasFailMessage)
			{
				return null;
			}
			return this.ErrorCode switch
			{
				Scp079HudTranslation.Zoom => null, 
				Scp079HudTranslation.NotEnoughAux => base.GetNoAuxMessage(this._cost), 
				Scp079HudTranslation.LockdownCooldown => this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this.RemainingCooldown)), 
				_ => this._failMessage, 
			};
		}
	}

	public float AuxRegenMultiplier
	{
		get
		{
			if (this.RemainingLockdownDuration == 0f)
			{
				return 1f;
			}
			int accessTierIndex = base.TierManager.AccessTierIndex;
			int a = this._regenerationPerTier.Length - 1;
			return this._regenerationPerTier[Mathf.Min(a, accessTierIndex)];
		}
	}

	public string AuxReductionMessage { get; private set; }

	private Scp079HudTranslation ErrorCode
	{
		get
		{
			if (base.TierManager.AccessTierIndex < this._minimalTierIndex)
			{
				return Scp079HudTranslation.HigherTierRequired;
			}
			if (!this._roomDoors.Any((DoorVariant x) => this.ValidateDoor(x)))
			{
				return Scp079HudTranslation.LockdownNoDoorsError;
			}
			if (this.RemainingCooldown > 0f)
			{
				return Scp079HudTranslation.LockdownCooldown;
			}
			if (base.AuxManager.CurrentAuxFloored < this._cost)
			{
				return Scp079HudTranslation.NotEnoughAux;
			}
			return Scp079HudTranslation.Zoom;
		}
	}

	private float RemainingCooldown
	{
		get
		{
			return Mathf.Max(0f, (float)(this._nextUseTime - NetworkTime.time));
		}
		set
		{
			this._nextUseTime = NetworkTime.time + (double)value;
		}
	}

	private float RemainingLockdownDuration => Mathf.Max(0f, (float)(this._nextUseTime - (double)this._cooldown - NetworkTime.time));

	public static event Action<Scp079Role, RoomIdentifier> OnServerLockdown;

	public static event Action<Scp079Role, DoorVariant> OnServerDoorLocked;

	private void ServerInitLockdown()
	{
		this._lockdownInEffect = true;
		this._lastLockedRoom = base.CurrentCamSync.CurrentCamera.Room;
		this._doorsToLockDown.UnionWith(this._roomDoors);
		Scp079LockdownRoomAbility.OnServerLockdown?.Invoke(base.CastRole, this._lastLockedRoom);
	}

	private void ServerCancelLockdown()
	{
		Scp079CancellingRoomLockdownEventArgs e = new Scp079CancellingRoomLockdownEventArgs(base.Owner, this._lastLockedRoom);
		Scp079Events.OnCancellingRoomLockdown(e);
		if (!e.IsAllowed)
		{
			return;
		}
		this._lockdownInEffect = false;
		this.RemainingCooldown = this._cooldown;
		foreach (DoorVariant item in this._alreadyLockedDown)
		{
			item.ServerChangeLock(DoorLockReason.Lockdown079, newState: false);
		}
		this._doorsToLockDown.Clear();
		this._alreadyLockedDown.Clear();
		base.ServerSendRpc(toAll: true);
		Scp079Events.OnCancelledRoomLockdown(new Scp079CancelledRoomLockdownEventArgs(base.Owner, this._lastLockedRoom));
	}

	private bool ValidateDoor(DoorVariant dv)
	{
		Scp079Camera currentCamera = base.CurrentCamSync.CurrentCamera;
		if (Scp079DoorAbility.ValidateAction(DoorAction.Closed, dv, currentCamera))
		{
			return Scp079DoorAbility.ValidateAction(DoorAction.Locked, dv, currentCamera);
		}
		return false;
	}

	protected override void Start()
	{
		base.Start();
		this._nameFormat = Translations.Get(Scp079HudTranslation.Lockdown);
		this._unlockText = Translations.Get(Scp079HudTranslation.LockdownAvailable);
		this.AuxReductionMessage = Translations.Get(Scp079HudTranslation.LockdownAuxPause);
		base.CurrentCamSync.OnCameraChanged += delegate
		{
			this._hasFailMessage = false;
			this._failMessage = null;
			this._roomDoors.Clear();
			if (DoorVariant.DoorsByRoom.TryGetValue(base.CurrentCamSync.CurrentCamera.Room, out var value))
			{
				this._roomDoors.UnionWith(value);
			}
		};
		base.GetSubroutine<Scp079DoorLockChanger>(out this._doorLockChanger);
	}

	protected override void Update()
	{
		base.Update();
		if (!this._lockdownInEffect || !NetworkServer.active)
		{
			return;
		}
		if (this.RemainingLockdownDuration <= 0f)
		{
			this.ServerCancelLockdown();
			return;
		}
		foreach (DoorVariant item in this._doorsToLockDown)
		{
			if (this.ValidateDoor(item) && !this._alreadyLockedDown.Contains(item) && (!item.TargetState || !(item.GetExactState() < this._minStateToClose)))
			{
				item.NetworkTargetState = false;
				item.ServerChangeLock(DoorLockReason.Lockdown079, newState: true);
				if (item == this._doorLockChanger.LockedDoor)
				{
					this._doorLockChanger.ServerUnlock();
				}
				base.RewardManager.MarkRooms(item.Rooms);
				Scp079LockdownRoomAbility.OnServerDoorLocked?.Invoke(base.CastRole, item);
				this._alreadyLockedDown.Add(item);
			}
		}
	}

	protected override void Trigger()
	{
		base.ClientSendCmd();
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.ErrorCode == Scp079HudTranslation.Zoom && !base.LostSignalHandler.Lost)
		{
			Scp079LockingDownRoomEventArgs e = new Scp079LockingDownRoomEventArgs(base.Owner, base.CurrentCamSync.CurrentCamera.Room);
			Scp079Events.OnLockingDownRoom(e);
			if (!e.IsAllowed)
			{
				return;
			}
			base.AuxManager.CurrentAux -= this._cost;
			this.RemainingCooldown = this._lockdownDuration + this._cooldown;
			this.ServerInitLockdown();
			Scp079Events.OnLockedDownRoom(new Scp079LockedDownRoomEventArgs(base.Owner, base.CurrentCamSync.CurrentCamera.Room));
		}
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteDouble(this._nextUseTime);
		writer.WriteBool(this._lockdownInEffect);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._nextUseTime = reader.ReadDouble();
		this._doorLockChanger.PlayConfirmationSound(reader.ReadBool() ? this._lockdownStartSound : this._lockdownEndSound);
	}

	public override void OnFailMessageAssigned()
	{
		base.OnFailMessageAssigned();
		this._hasFailMessage = true;
		this._failMessage = Translations.Get(this.ErrorCode);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._nextUseTime = 0.0;
		if (NetworkServer.active)
		{
			this.ServerCancelLockdown();
		}
	}

	public static bool IsLockedDown(DoorVariant dv)
	{
		return ((DoorLockReason)dv.ActiveLocks).HasFlagFast(DoorLockReason.Lockdown079);
	}

	public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
	{
		if (newLevel != this._minimalTierIndex)
		{
			return false;
		}
		sb.AppendFormat(this._unlockText, $"[{new ReadableKeyCode(this.ActivationKey)}]");
		return true;
	}
}
