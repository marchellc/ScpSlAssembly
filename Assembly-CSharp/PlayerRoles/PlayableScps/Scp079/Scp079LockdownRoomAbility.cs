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

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079LockdownRoomAbility : Scp079KeyAbilityBase, IScp079LevelUpNotifier, IScp079AuxRegenModifier
	{
		public static event Action<Scp079Role, RoomIdentifier> OnServerLockdown;

		public static event Action<Scp079Role, DoorVariant> OnServerDoorLocked;

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079Lockdown;
			}
		}

		public override bool IsReady
		{
			get
			{
				return this.ErrorCode == Scp079HudTranslation.Zoom;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return !Scp079CursorManager.LockCameras && this.ErrorCode != Scp079HudTranslation.HigherTierRequired;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._nameFormat, this._cost);
			}
		}

		public override string FailMessage
		{
			get
			{
				if (!this._hasFailMessage)
				{
					return null;
				}
				Scp079HudTranslation errorCode = this.ErrorCode;
				if (errorCode == Scp079HudTranslation.Zoom)
				{
					return null;
				}
				if (errorCode == Scp079HudTranslation.NotEnoughAux)
				{
					return base.GetNoAuxMessage((float)this._cost);
				}
				if (errorCode != Scp079HudTranslation.LockdownCooldown)
				{
					return this._failMessage;
				}
				return this._failMessage + "\n" + base.AuxManager.GenerateCustomETA(Mathf.CeilToInt(this.RemainingCooldown));
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
				int num = this._regenerationPerTier.Length - 1;
				return this._regenerationPerTier[Mathf.Min(num, accessTierIndex)];
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

		private float RemainingLockdownDuration
		{
			get
			{
				return Mathf.Max(0f, (float)(this._nextUseTime - (double)this._cooldown - NetworkTime.time));
			}
		}

		private void ServerInitLockdown()
		{
			this._lockdownInEffect = true;
			this._lastLockedRoom = base.CurrentCamSync.CurrentCamera.Room;
			this._doorsToLockDown.UnionWith(this._roomDoors);
			Action<Scp079Role, RoomIdentifier> onServerLockdown = Scp079LockdownRoomAbility.OnServerLockdown;
			if (onServerLockdown == null)
			{
				return;
			}
			onServerLockdown(base.CastRole, this._lastLockedRoom);
		}

		private void ServerCancelLockdown()
		{
			Scp079CancellingRoomLockdownEventArgs scp079CancellingRoomLockdownEventArgs = new Scp079CancellingRoomLockdownEventArgs(base.Owner, this._lastLockedRoom);
			Scp079Events.OnCancellingRoomLockdown(scp079CancellingRoomLockdownEventArgs);
			if (!scp079CancellingRoomLockdownEventArgs.IsAllowed)
			{
				return;
			}
			this._lockdownInEffect = false;
			this.RemainingCooldown = this._cooldown;
			foreach (DoorVariant doorVariant in this._alreadyLockedDown)
			{
				doorVariant.ServerChangeLock(DoorLockReason.Lockdown079, false);
			}
			this._doorsToLockDown.Clear();
			this._alreadyLockedDown.Clear();
			base.ServerSendRpc(true);
			Scp079Events.OnCancelledRoomLockdown(new Scp079CancelledRoomLockdownEventArgs(base.Owner, this._lastLockedRoom));
		}

		private bool ValidateDoor(DoorVariant dv)
		{
			Scp079Camera currentCamera = base.CurrentCamSync.CurrentCamera;
			return Scp079DoorAbility.ValidateAction(DoorAction.Closed, dv, currentCamera) && Scp079DoorAbility.ValidateAction(DoorAction.Locked, dv, currentCamera);
		}

		protected override void Start()
		{
			base.Start();
			this._nameFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.Lockdown);
			this._unlockText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.LockdownAvailable);
			this.AuxReductionMessage = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.LockdownAuxPause);
			base.CurrentCamSync.OnCameraChanged += delegate
			{
				this._hasFailMessage = false;
				this._failMessage = null;
				this._roomDoors.Clear();
				HashSet<DoorVariant> hashSet;
				if (DoorVariant.DoorsByRoom.TryGetValue(base.CurrentCamSync.CurrentCamera.Room, out hashSet))
				{
					this._roomDoors.UnionWith(hashSet);
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
			foreach (DoorVariant doorVariant in this._doorsToLockDown)
			{
				if (this.ValidateDoor(doorVariant) && !this._alreadyLockedDown.Contains(doorVariant) && (!doorVariant.TargetState || doorVariant.GetExactState() >= this._minStateToClose))
				{
					doorVariant.NetworkTargetState = false;
					doorVariant.ServerChangeLock(DoorLockReason.Lockdown079, true);
					if (doorVariant == this._doorLockChanger.LockedDoor)
					{
						this._doorLockChanger.ServerUnlock();
					}
					base.RewardManager.MarkRooms(doorVariant.Rooms);
					Action<Scp079Role, DoorVariant> onServerDoorLocked = Scp079LockdownRoomAbility.OnServerDoorLocked;
					if (onServerDoorLocked != null)
					{
						onServerDoorLocked(base.CastRole, doorVariant);
					}
					this._alreadyLockedDown.Add(doorVariant);
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
				Scp079LockingDownRoomEventArgs scp079LockingDownRoomEventArgs = new Scp079LockingDownRoomEventArgs(base.Owner, base.CurrentCamSync.CurrentCamera.Room);
				Scp079Events.OnLockingDownRoom(scp079LockingDownRoomEventArgs);
				if (!scp079LockingDownRoomEventArgs.IsAllowed)
				{
					return;
				}
				base.AuxManager.CurrentAux -= (float)this._cost;
				this.RemainingCooldown = this._lockdownDuration + this._cooldown;
				this.ServerInitLockdown();
				Scp079Events.OnLockedDownRoom(new Scp079LockedDownRoomEventArgs(base.Owner, base.CurrentCamSync.CurrentCamera.Room));
			}
			base.ServerSendRpc(true);
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
			this._failMessage = Translations.Get<Scp079HudTranslation>(this.ErrorCode);
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
			sb.AppendFormat(this._unlockText, string.Format("[{0}]", new ReadableKeyCode(this.ActivationKey)));
			return true;
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

		private enum ValidationError
		{
			None,
			Unknown,
			NotEnoughAux = 6,
			TierTooLow = 8,
			Cooldown = 31,
			NoDoors
		}
	}
}
