using CustomPlayerEffects;
using InventorySystem.Disarming;
using InventorySystem.Drawers;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1344;

public class Scp1344Item : UsableItem, IWearableItem, IItemProgressbarDrawer, IItemDrawer
{
	private const ActionName InspectKey = ActionName.InspectItem;

	private const float EquipTime = 1.3f;

	private const float DeactivationTime = 5.1f;

	private const float DeactivationTransitionTime = 0.5f;

	private const float StabTime = 0.065f;

	private const float ActivationTime = 5f;

	private const float ActivationTransitionTime = 1f;

	private const float ActivationItemDeselectionTime = 0.75f;

	private const float InspectionCooldown = 4f;

	private const byte ActivationBlindnessIntensity = 100;

	private const byte LockedBlindnessIntensity = 101;

	private const byte MinBlindnessIntensity = 15;

	private const float BarWidth = 650f;

	private Scp1344Status _status;

	private float _useTime;

	private float _cancelationTime;

	private byte _savedIntensity;

	private double _nextInspectTime;

	private int _lastLifeId;

	public CustomPlayerEffects.Scp1344 Scp1344Effect => base.Owner.playerEffectsController.GetEffect<CustomPlayerEffects.Scp1344>();

	public Blindness BlindnessEffect => base.Owner.playerEffectsController.GetEffect<Blindness>();

	public SeveredEyes SeveredEyesEffect => base.Owner.playerEffectsController.GetEffect<SeveredEyes>();

	public bool IsWorn
	{
		get
		{
			Scp1344Status status = this.Status;
			return status == Scp1344Status.Active || status == Scp1344Status.Deactivating;
		}
	}

	public WearableSlot Slot => WearableSlot.Eyes;

	public override bool CanStartUsing
	{
		get
		{
			if (!base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
			{
				if (this.Status != Scp1344Status.Deactivating)
				{
					if (this.Status == Scp1344Status.Idle && !this.Scp1344Effect.IsEnabled)
					{
						return this.BlindnessEffect.Intensity <= 100;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public override bool AllowEquip => this.Status != Scp1344Status.Active;

	public override bool AllowHolster
	{
		get
		{
			if (this.Status == Scp1344Status.Deactivating)
			{
				return !base.IsEquipped;
			}
			return true;
		}
	}

	public bool AllowInspect
	{
		get
		{
			if (this.Status == Scp1344Status.Idle)
			{
				return this._nextInspectTime < NetworkTime.time;
			}
			return false;
		}
	}

	public bool ProgressbarEnabled => this.Status == Scp1344Status.Deactivating;

	public float ProgressbarMin => 0f;

	public float ProgressbarMax => 5.1f;

	public float ProgressbarValue
	{
		get
		{
			if (!NetworkServer.active)
			{
				return Time.time - this._useTime;
			}
			return this._useTime;
		}
	}

	public float ProgressbarWidth => 650f;

	public Scp1344Status Status
	{
		get
		{
			if (this.CurLifeId != this._lastLifeId)
			{
				this._status = Scp1344Status.Idle;
				this._lastLifeId = this.CurLifeId;
			}
			return this._status;
		}
		set
		{
			this._status = value;
			this._lastLifeId = this.CurLifeId;
			if (NetworkServer.active)
			{
				this.ServerChangeStatus(value);
			}
			else
			{
				this.ClientChangeStatus(value);
			}
		}
	}

	private int CurLifeId => base.Owner.roleManager.CurrentRole.UniqueLifeIdentifier;

	public override void ServerOnUsingCompleted()
	{
		if (this.Status == Scp1344Status.Idle)
		{
			this.ServerSetStatus(Scp1344Status.Equipping);
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (NetworkServer.active)
		{
			Scp1344Status status = this.Status;
			if (status != Scp1344Status.Stabbing && status != Scp1344Status.Active && status != Scp1344Status.Dropping && status != Scp1344Status.CancelingDeactivation)
			{
				this.ServerSetStatus(Scp1344Status.Idle);
			}
		}
	}

	public override ItemPickupBase ServerDropItem(bool spawn)
	{
		Scp1344Status status = this.Status;
		if (status == Scp1344Status.Deactivating || status == Scp1344Status.CancelingDeactivation)
		{
			return null;
		}
		if (this.Status != Scp1344Status.Active)
		{
			return base.ServerDropItem(spawn);
		}
		this.ServerSetStatus(Scp1344Status.Dropping);
		return null;
	}

	public override void OnEquipped()
	{
		base.OnEquipped();
		if (NetworkServer.active && this.Status == Scp1344Status.Dropping)
		{
			this.ServerSetStatus(Scp1344Status.Deactivating);
		}
	}

	public override void OnUsingStarted()
	{
		if (NetworkServer.active && this.Status == Scp1344Status.Deactivating)
		{
			this.ServerSetStatus(Scp1344Status.CancelingDeactivation);
		}
		else
		{
			base.OnUsingStarted();
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (Input.GetKeyDown(NewInput.GetKey(ActionName.InspectItem)) && this.AllowInspect)
		{
			NetworkClient.Send(new Scp1344StatusMessage(base.ItemSerial, Scp1344Status.Inspecting));
		}
	}

	private void OnPlayerDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub)
	{
		if (!(targetHub != base.Owner))
		{
			base.ServerDropItem(spawn: true);
			Scp1344Status status = this.Status;
			if (status != Scp1344Status.Idle && status != Scp1344Status.Inspecting && status != Scp1344Status.Equipping)
			{
				this.ActivateFinalEffects();
			}
		}
	}

	private void OnAnyPlayerDied(ReferenceHub hub, DamageHandlerBase _)
	{
		if (!(hub != base.Owner))
		{
			this.Status = Scp1344Status.Idle;
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			switch (this.Status)
			{
			case Scp1344Status.Equipping:
				this.ServerUpdateTimedStatus(1.3f, Scp1344Status.Activating);
				break;
			case Scp1344Status.Activating:
				this.ServerUpdateTimedStatus(5f, Scp1344Status.Stabbing);
				break;
			case Scp1344Status.Stabbing:
				this.ServerUpdateTimedStatus(0.065f, Scp1344Status.Active);
				break;
			case Scp1344Status.Active:
				this.ServerUpdateActive();
				break;
			case Scp1344Status.Deactivating:
				this.ServerUpdateDeactivating();
				break;
			case Scp1344Status.CancelingDeactivation:
				this.ServerUpdateTimedStatus(0.5f, Scp1344Status.Active);
				break;
			case Scp1344Status.Dropping:
				break;
			}
		}
	}

	private void ServerUpdateTimedStatus(float time, Scp1344Status status)
	{
		this._useTime += Time.deltaTime;
		if (!(this._useTime < time))
		{
			switch (this.Status)
			{
			case Scp1344Status.Stabbing:
				this._savedIntensity = 100;
				break;
			case Scp1344Status.CancelingDeactivation:
				base.OwnerInventory.ServerSelectItem(0);
				break;
			}
			this.ServerSetStatus(status);
		}
	}

	private void ServerUpdateActive()
	{
		if (this.BlindnessEffect.Intensity <= 15)
		{
			return;
		}
		this._useTime += Time.deltaTime;
		float useTime = this._useTime;
		if (useTime < 0.75f)
		{
			return;
		}
		if (useTime < 1f)
		{
			if (base.IsEquipped)
			{
				base.OwnerInventory.ServerSelectItem(0);
			}
		}
		else
		{
			float num = 100f - 20f * Mathf.Pow(this._useTime, 0.385f);
			this.BlindnessEffect.Intensity = (byte)num;
		}
	}

	private void ServerUpdateDeactivating()
	{
		this._useTime += Time.deltaTime;
		float useTime = this._useTime;
		if (!(useTime < 0.5f))
		{
			if (useTime < 5.1f)
			{
				this.BlindnessEffect.Intensity = this._savedIntensity;
				return;
			}
			this.ActivateFinalEffects();
			base.ServerDropItem(spawn: true);
		}
	}

	private void ActivateFinalEffects()
	{
		this.BlindnessEffect.Intensity = 101;
		this.Scp1344Effect.IsEnabled = false;
		this.SeveredEyesEffect.IsEnabled = true;
	}

	private void Awake()
	{
		Scp1344NetworkHandler.OnStatusChanged += OnStatusChanged;
		DisarmingHandlers.OnPlayerDisarmed += OnPlayerDisarmed;
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp1344NetworkHandler.OnStatusChanged -= OnStatusChanged;
		DisarmingHandlers.OnPlayerDisarmed -= OnPlayerDisarmed;
		PlayerStats.OnAnyPlayerDied -= OnAnyPlayerDied;
	}

	private void OnStatusChanged(ushort serial, Scp1344Status status)
	{
		if (base.ItemSerial == serial)
		{
			this.Status = status;
			if (status == Scp1344Status.Dropping)
			{
				this.Scp1344Effect.PlayBuildupSound();
			}
		}
	}

	private void ServerSetStatus(Scp1344Status status)
	{
		Scp1344NetworkHandler.ServerSendMessage(new Scp1344StatusMessage(base.ItemSerial, status));
		switch (status)
		{
		case Scp1344Status.Activating:
			base.Owner.EnableWearables(WearableElements.Scp1344Goggles);
			break;
		case Scp1344Status.Idle:
			base.Owner.DisableWearables(WearableElements.Scp1344Goggles);
			break;
		}
	}

	private void ClientChangeStatus(Scp1344Status status)
	{
		switch (status)
		{
		case Scp1344Status.Deactivating:
			base.IsUsing = false;
			this._useTime = Time.time;
			break;
		case Scp1344Status.Inspecting:
			this._nextInspectTime = NetworkTime.time + 4.0;
			break;
		}
	}

	private void ServerChangeStatus(Scp1344Status status)
	{
		switch (status)
		{
		case Scp1344Status.Idle:
		case Scp1344Status.Equipping:
		case Scp1344Status.Activating:
		case Scp1344Status.Stabbing:
		case Scp1344Status.Deactivating:
			base.IsUsing = false;
			this._useTime = 0f;
			break;
		case Scp1344Status.Active:
			this._useTime = this._cancelationTime;
			this._cancelationTime = 0f;
			this.Scp1344Effect.IsEnabled = true;
			this.BlindnessEffect.Intensity = this._savedIntensity;
			break;
		case Scp1344Status.Dropping:
			this._cancelationTime = this._useTime;
			this._savedIntensity = this.BlindnessEffect.Intensity;
			this.BlindnessEffect.Intensity = 101;
			if (base.OwnerInventory.CurItem.SerialNumber == base.ItemSerial)
			{
				this.ServerSetStatus(Scp1344Status.Deactivating);
			}
			else
			{
				base.OwnerInventory.ServerSelectItem(base.ItemSerial);
			}
			break;
		case Scp1344Status.Inspecting:
			this._nextInspectTime = NetworkTime.time + 4.0;
			break;
		case Scp1344Status.CancelingDeactivation:
			this._useTime = 0f;
			this._savedIntensity = this.BlindnessEffect.Intensity;
			this.BlindnessEffect.Intensity = 100;
			break;
		}
	}
}
