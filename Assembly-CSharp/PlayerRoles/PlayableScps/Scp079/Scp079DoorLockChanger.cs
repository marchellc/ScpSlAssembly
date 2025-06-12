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

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079DoorLockChanger : Scp079DoorAbility, IPoolResettable, IScp079AuxRegenModifier, IScp079LevelUpNotifier
{
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

	public override ActionName ActivationKey => ActionName.Scp079LockDoor;

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

	public override string AbilityName => string.Format(this.IsHighlightLockedBy079 ? Scp079DoorLockChanger._unlockText : Scp079DoorLockChanger._lockText, this.GetCostForDoor(this.TargetAction, base.LastDoor));

	public override bool IsReady
	{
		get
		{
			if (base.TierManager.AccessTierIndex < this._minTierIndex)
			{
				return false;
			}
			if (!base.IsReady)
			{
				return false;
			}
			if (this.TargetAction == DoorAction.Unlocked)
			{
				return true;
			}
			if (this.LockedDoor == null && !Scp079LockdownRoomAbility.IsLockedDown(base.LastDoor))
			{
				return this._cooldown.IsReady;
			}
			return false;
		}
	}

	public override bool IsVisible
	{
		get
		{
			if (base.IsVisible)
			{
				return base.TierManager.AccessTierIndex >= this._minTierIndex;
			}
			return false;
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
			int secondsRemaining = Mathf.CeilToInt(this._cooldown.Remaining);
			return Scp079DoorLockChanger._cooldownText + "\n" + base.AuxManager.GenerateCustomETA(secondsRemaining);
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

	[field: SerializeField]
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

	private bool IsHighlightLockedBy079 => ((DoorLockReason)base.LastDoor.ActiveLocks).HasFlagFast(DoorLockReason.Regular079);

	public static event Action<Scp079Role, DoorVariant> OnServerDoorLocked;

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
		Scp079DoorLockChanger._lockText = Translations.Get(Scp079HudTranslation.LockDoor);
		Scp079DoorLockChanger._unlockText = Translations.Get(Scp079HudTranslation.UnlockDoor);
		Scp079DoorLockChanger._cooldownText = Translations.Get(Scp079HudTranslation.DoorLockCooldown);
		Scp079DoorLockChanger._alreadyLockedText = Translations.Get(Scp079HudTranslation.DoorLockAlreadyActive);
		this.AuxReductionMessage = Translations.Get(Scp079HudTranslation.DoorLockAuxPause);
		Scp079DoorLockChanger._abilityUnlockText = Translations.Get(Scp079HudTranslation.DoorLockAbilityAvailable);
		base.LostSignalHandler.OnStatusChanged += delegate
		{
			if (NetworkServer.active && base.LostSignalHandler.Lost)
			{
				this.ServerUnlock();
			}
		};
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active && !(this.LockedDoor == null))
		{
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
			if (!(num > 0f) || !Scp079DoorAbility.ValidateAction(DoorAction.Locked, this.LockedDoor, base.CurrentCamSync.CurrentCamera))
			{
				this.ServerUnlock();
			}
		}
	}

	public void ServerUnlock()
	{
		if (NetworkServer.active && !(this.LockedDoor == null))
		{
			Scp079UnlockingDoorEventArgs e = new Scp079UnlockingDoorEventArgs(base.Owner, this.LockedDoor);
			Scp079Events.OnUnlockingDoor(e);
			if (e.IsAllowed)
			{
				double num = (NetworkTime.time - this._lockTime) * (double)this._cooldownPerTimeLocked;
				this._cooldown.Trigger(num + (double)this._cooldownBaseline);
				this.LockedDoor.ServerChangeLock(DoorLockReason.Regular079, newState: false);
				Scp079Events.OnUnlockedDoor(new Scp079UnlockedDoorEventArgs(base.Owner, this.LockedDoor));
				this.LockedDoor = null;
				base.ServerSendRpc(toAll: true);
			}
		}
	}

	public override void OnFailMessageAssigned()
	{
		base.OnFailMessageAssigned();
		this._failedDoor = base.LastDoor;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteUInt(base.LastDoor.netId);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (!NetworkServer.spawned.TryGetValue(reader.ReadUInt(), out var value) || !value.TryGetComponent<DoorVariant>(out base.LastDoor) || !this.IsReady)
		{
			return;
		}
		if (this.TargetAction == DoorAction.Locked)
		{
			if (base.LostSignalHandler.Lost)
			{
				return;
			}
			Scp079LockingDoorEventArgs e = new Scp079LockingDoorEventArgs(base.Owner, base.LastDoor);
			Scp079Events.OnLockingDoor(e);
			if (!e.IsAllowed)
			{
				return;
			}
			this._lockTime = NetworkTime.time;
			this.LockedDoor = base.LastDoor;
			this.LockedDoor.ServerChangeLock(DoorLockReason.Regular079, newState: true);
			base.RewardManager.MarkRooms(base.LastDoor.Rooms);
			Scp079DoorLockChanger.OnServerDoorLocked?.Invoke(base.CastRole, base.LastDoor);
			base.AuxManager.CurrentAux -= this.GetCostForDoor(DoorAction.Locked, base.LastDoor);
			Scp079Events.OnLockedDoor(new Scp079LockedDoorEventArgs(base.Owner, base.LastDoor));
		}
		else if (base.LastDoor == this.LockedDoor)
		{
			this.ServerUnlock();
		}
		base.ServerSendRpc(toAll: true);
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
		if (!(doorVariant == lockedDoor) || NetworkServer.active)
		{
			this.LockedDoor = doorVariant;
			if (doorVariant == null)
			{
				base.PlayConfirmationSound(this._unlockSound);
				return;
			}
			new Scp079DoorWhir(base.CastRole, this._whirSound);
			base.PlayConfirmationSound(this._lockSound);
		}
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
		sb.AppendFormat(Scp079DoorLockChanger._abilityUnlockText, $"[{new ReadableKeyCode(this.ActivationKey)}]");
		return true;
	}
}
