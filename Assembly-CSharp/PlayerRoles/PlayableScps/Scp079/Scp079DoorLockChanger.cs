using System;
using System.Text;
using GameObjectPools;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079DoorLockChanger : Scp079DoorAbility, IPoolResettable, IScp079AuxRegenModifier, IScp079LevelUpNotifier
	{
		public static event Action<Scp079Role, DoorVariant> OnServerDoorLocked;

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079LockDoor;
			}
		}

		public float AuxRegenMultiplier
		{
			get
			{
				if (!(this.LockedDoor != null))
				{
					return 1f;
				}
				return 0f;
			}
		}

		public string AuxReductionMessage { get; private set; }

		public DoorVariant LockedDoor { get; private set; }

		public override string AbilityName
		{
			get
			{
				return string.Format(this.IsHighlightLockedBy079 ? Scp079DoorLockChanger._unlockText : Scp079DoorLockChanger._lockText, this.GetCostForDoor(this.TargetAction, this.LastDoor));
			}
		}

		public override bool IsReady
		{
			get
			{
				return base.TierManager.AccessTierIndex >= this._minTierIndex && base.IsReady && (this.TargetAction == DoorAction.Unlocked || (this.LockedDoor == null && !Scp079LockdownRoomAbility.IsLockedDown(this.LastDoor) && this._cooldown.IsReady));
			}
		}

		public override bool IsVisible
		{
			get
			{
				return base.IsVisible && base.TierManager.AccessTierIndex >= this._minTierIndex;
			}
		}

		public override string FailMessage
		{
			get
			{
				if (this._failedDoor == null)
				{
					return null;
				}
				if (this.LockedDoor != null && this._failedDoor != this.LockedDoor)
				{
					return Scp079DoorLockChanger._alreadyLockedText;
				}
				if (Scp079LockdownRoomAbility.IsLockedDown(this._failedDoor))
				{
					return Scp079DoorLockChanger._alreadyLockedText;
				}
				if (this._cooldown.Remaining == 0f)
				{
					return base.FailMessage;
				}
				int num = Mathf.CeilToInt(this._cooldown.Remaining);
				return Scp079DoorLockChanger._cooldownText + "\n" + base.AuxManager.GenerateCustomETA(num);
			}
		}

		public int LockClosedDoorCost
		{
			get
			{
				int num = (int)(base.AuxManager.MaxAux * this._costMaxAuxPercent);
				int num2 = num % this._costRounding;
				return num + num2;
			}
		}

		public int LockOpenDoorCost { get; private set; }

		protected override DoorAction TargetAction
		{
			get
			{
				if (!this.IsHighlightLockedBy079)
				{
					return DoorAction.Locked;
				}
				return DoorAction.Unlocked;
			}
		}

		private bool IsHighlightLockedBy079
		{
			get
			{
				return ((DoorLockReason)this.LastDoor.ActiveLocks).HasFlagFast(DoorLockReason.Regular079);
			}
		}

		protected override int GetCostForDoor(DoorAction action, DoorVariant door)
		{
			if (action != DoorAction.Locked)
			{
				return 0;
			}
			if (!door.TargetState)
			{
				return this.LockClosedDoorCost;
			}
			return this.LockOpenDoorCost;
		}

		protected virtual void OnDestroy()
		{
			this.ServerUnlock();
		}

		protected override void Start()
		{
			base.Start();
			Scp079DoorLockChanger._lockText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.LockDoor);
			Scp079DoorLockChanger._unlockText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.UnlockDoor);
			Scp079DoorLockChanger._cooldownText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.DoorLockCooldown);
			Scp079DoorLockChanger._alreadyLockedText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.DoorLockAlreadyActive);
			this.AuxReductionMessage = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.DoorLockAuxPause);
			Scp079DoorLockChanger._abilityUnlockText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.DoorLockAbilityAvailable);
			base.LostSignalHandler.OnStatusChanged += delegate
			{
				if (!NetworkServer.active || !base.LostSignalHandler.Lost)
				{
					return;
				}
				this.ServerUnlock();
			};
		}

		protected override void Update()
		{
			base.Update();
			if (!NetworkServer.active || this.LockedDoor == null)
			{
				return;
			}
			float num = base.AuxManager.CurrentAux;
			if (this.LockedDoor.TargetState)
			{
				this._lockTime = NetworkTime.time;
			}
			else
			{
				float num2 = (float)(NetworkTime.time - this._lockTime);
				num -= Mathf.Pow(num2 * this._lockCostPerSec, this._lockCostPow) * Time.deltaTime;
				base.AuxManager.CurrentAux = num;
			}
			if (num > 0f && Scp079DoorAbility.ValidateAction(DoorAction.Locked, this.LockedDoor, base.CurrentCamSync.CurrentCamera))
			{
				return;
			}
			this.ServerUnlock();
		}

		public void ServerUnlock()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (this.LockedDoor == null)
			{
				return;
			}
			Scp079UnlockingDoorEventArgs scp079UnlockingDoorEventArgs = new Scp079UnlockingDoorEventArgs(base.Owner, this.LockedDoor);
			Scp079Events.OnUnlockingDoor(scp079UnlockingDoorEventArgs);
			if (!scp079UnlockingDoorEventArgs.IsAllowed)
			{
				return;
			}
			double num = (NetworkTime.time - this._lockTime) * (double)this._cooldownPerTimeLocked;
			this._cooldown.Trigger(num + (double)this._cooldownBaseline);
			this.LockedDoor.ServerChangeLock(DoorLockReason.Regular079, false);
			Scp079Events.OnUnlockedDoor(new Scp079UnlockedDoorEventArgs(base.Owner, this.LockedDoor));
			this.LockedDoor = null;
			base.ServerSendRpc(true);
		}

		public override void OnFailMessageAssigned()
		{
			base.OnFailMessageAssigned();
			this._failedDoor = this.LastDoor;
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteUInt(this.LastDoor.netId);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			NetworkIdentity networkIdentity;
			if (!NetworkServer.spawned.TryGetValue(reader.ReadUInt(), out networkIdentity))
			{
				return;
			}
			if (!networkIdentity.TryGetComponent<DoorVariant>(out this.LastDoor) || !this.IsReady)
			{
				return;
			}
			if (this.TargetAction == DoorAction.Locked)
			{
				if (base.LostSignalHandler.Lost)
				{
					return;
				}
				Scp079LockingDoorEventArgs scp079LockingDoorEventArgs = new Scp079LockingDoorEventArgs(base.Owner, this.LastDoor);
				Scp079Events.OnLockingDoor(scp079LockingDoorEventArgs);
				if (!scp079LockingDoorEventArgs.IsAllowed)
				{
					return;
				}
				this._lockTime = NetworkTime.time;
				this.LockedDoor = this.LastDoor;
				this.LockedDoor.ServerChangeLock(DoorLockReason.Regular079, true);
				base.RewardManager.MarkRooms(this.LastDoor.Rooms);
				Action<Scp079Role, DoorVariant> onServerDoorLocked = Scp079DoorLockChanger.OnServerDoorLocked;
				if (onServerDoorLocked != null)
				{
					onServerDoorLocked(base.CastRole, this.LastDoor);
				}
				base.AuxManager.CurrentAux -= (float)this.GetCostForDoor(DoorAction.Locked, this.LastDoor);
				Scp079Events.OnLockedDoor(new Scp079LockedDoorEventArgs(base.Owner, this.LastDoor));
			}
			else if (this.LastDoor == this.LockedDoor)
			{
				this.ServerUnlock();
			}
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this._cooldown.WriteCooldown(writer);
			writer.WriteNetworkBehaviour(this.LockedDoor);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._failedDoor = null;
			this._cooldown.ReadCooldown(reader);
			DoorVariant lockedDoor = this.LockedDoor;
			DoorVariant doorVariant = reader.ReadNetworkBehaviour<DoorVariant>();
			if (doorVariant == lockedDoor && !NetworkServer.active)
			{
				return;
			}
			this.LockedDoor = doorVariant;
			if (doorVariant == null)
			{
				base.PlayConfirmationSound(this._unlockSound);
				return;
			}
			new Scp079DoorWhir(base.CastRole, this._whirSound);
			base.PlayConfirmationSound(this._lockSound);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.ServerUnlock();
			this._cooldown.Clear();
		}

		public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
		{
			if (newLevel != this._minTierIndex)
			{
				return false;
			}
			sb.Append(Scp079DoorLockChanger._abilityUnlockText);
			return true;
		}

		[SerializeField]
		private AudioClip _lockSound;

		[SerializeField]
		private AudioClip _unlockSound;

		[SerializeField]
		private float _costMaxAuxPercent;

		[SerializeField]
		private int _costRounding;

		[SerializeField]
		private float _cooldownBaseline;

		[SerializeField]
		private float _cooldownPerTimeLocked;

		[SerializeField]
		private float _lockCostPerSec;

		[SerializeField]
		private float _lockCostPow;

		[SerializeField]
		private AudioClip _whirSound;

		[SerializeField]
		private int _minTierIndex;

		private static string _lockText;

		private static string _unlockText;

		private static string _cooldownText;

		private static string _alreadyLockedText;

		private static string _abilityUnlockText;

		private readonly AbilityCooldown _cooldown = new AbilityCooldown();

		private DoorVariant _failedDoor;

		private double _lockTime;

		private const DoorLockReason LockReason = DoorLockReason.Regular079;
	}
}
