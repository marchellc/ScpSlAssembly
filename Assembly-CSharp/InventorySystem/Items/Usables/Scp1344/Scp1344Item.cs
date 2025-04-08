using System;
using CustomPlayerEffects;
using InventorySystem.Disarming;
using InventorySystem.Drawers;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1344
{
	public class Scp1344Item : UsableItem, IWearableItem, IItemProgressbarDrawer, IItemDrawer
	{
		public Scp1344 Scp1344Effect
		{
			get
			{
				return base.Owner.playerEffectsController.GetEffect<Scp1344>();
			}
		}

		public Blindness BlindnessEffect
		{
			get
			{
				return base.Owner.playerEffectsController.GetEffect<Blindness>();
			}
		}

		public SeveredEyes SeveredEyesEffect
		{
			get
			{
				return base.Owner.playerEffectsController.GetEffect<SeveredEyes>();
			}
		}

		public bool IsWorn
		{
			get
			{
				Scp1344Status status = this.Status;
				return status == Scp1344Status.Active || status == Scp1344Status.Deactivating;
			}
		}

		public WearableSlot Slot
		{
			get
			{
				return WearableSlot.Eyes;
			}
		}

		public override bool CanStartUsing
		{
			get
			{
				return !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction) && (this.Status == Scp1344Status.Deactivating || (this.Status == Scp1344Status.Idle && !this.Scp1344Effect.IsEnabled && this.BlindnessEffect.Intensity <= 100));
			}
		}

		public override bool AllowEquip
		{
			get
			{
				return this.Status != Scp1344Status.Active;
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return this.Status != Scp1344Status.Deactivating || !base.IsEquipped;
			}
		}

		public bool AllowInspect
		{
			get
			{
				return this.Status == Scp1344Status.Idle && this._nextInspectTime < NetworkTime.time;
			}
		}

		public bool ProgressbarEnabled
		{
			get
			{
				return this.Status == Scp1344Status.Deactivating;
			}
		}

		public float ProgressbarMin
		{
			get
			{
				return 0f;
			}
		}

		public float ProgressbarMax
		{
			get
			{
				return 5.1f;
			}
		}

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

		public float ProgressbarWidth
		{
			get
			{
				return 650f;
			}
		}

		public Scp1344Status Status
		{
			get
			{
				return this._status;
			}
			set
			{
				this._status = value;
				if (NetworkServer.active)
				{
					this.ServerChangeStatus(value);
					return;
				}
				this.ClientChangeStatus(value);
			}
		}

		public override void ServerOnUsingCompleted()
		{
			if (this.Status != Scp1344Status.Idle)
			{
				return;
			}
			this.ServerSetStatus(Scp1344Status.Equipping);
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			if (!NetworkServer.active)
			{
				return;
			}
			Scp1344Status status = this.Status;
			if (status == Scp1344Status.Stabbing || status == Scp1344Status.Active || status == Scp1344Status.Dropping || status == Scp1344Status.CancelingDeactivation)
			{
				return;
			}
			this.ServerSetStatus(Scp1344Status.Idle);
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
			if (!NetworkServer.active)
			{
				return;
			}
			if (this.Status != Scp1344Status.Dropping)
			{
				return;
			}
			this.ServerSetStatus(Scp1344Status.Deactivating);
		}

		public override void OnUsingStarted()
		{
			if (NetworkServer.active && this.Status == Scp1344Status.Deactivating)
			{
				this.ServerSetStatus(Scp1344Status.CancelingDeactivation);
				return;
			}
			base.OnUsingStarted();
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!Input.GetKeyDown(NewInput.GetKey(ActionName.InspectItem, KeyCode.None)) || !this.AllowInspect)
			{
				return;
			}
			NetworkClient.Send<Scp1344StatusMessage>(new Scp1344StatusMessage(base.ItemSerial, Scp1344Status.Inspecting), 0);
		}

		private void OnPlayerDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			if (targetHub != base.Owner)
			{
				return;
			}
			base.ServerDropItem(true);
			Scp1344Status status = this.Status;
			if (status != Scp1344Status.Idle && status != Scp1344Status.Inspecting && status != Scp1344Status.Equipping)
			{
				this.ActivateFinalEffects();
			}
		}

		private void OnAnyPlayerDied(ReferenceHub hub, DamageHandlerBase _)
		{
			if (hub != base.Owner)
			{
				return;
			}
			this.Status = Scp1344Status.Idle;
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			switch (this.Status)
			{
			case Scp1344Status.Equipping:
				this.ServerUpdateTimedStatus(1.3f, Scp1344Status.Activating);
				return;
			case Scp1344Status.Activating:
				this.ServerUpdateTimedStatus(5f, Scp1344Status.Stabbing);
				return;
			case Scp1344Status.Stabbing:
				this.ServerUpdateTimedStatus(0.065f, Scp1344Status.Active);
				return;
			case Scp1344Status.Active:
				this.ServerUpdateActive();
				return;
			case Scp1344Status.Dropping:
				break;
			case Scp1344Status.Deactivating:
				this.ServerUpdateDeactivating();
				return;
			case Scp1344Status.CancelingDeactivation:
				this.ServerUpdateTimedStatus(0.5f, Scp1344Status.Active);
				break;
			default:
				return;
			}
		}

		private void ServerUpdateTimedStatus(float time, Scp1344Status status)
		{
			this._useTime += Time.deltaTime;
			if (this._useTime < time)
			{
				return;
			}
			Scp1344Status status2 = this.Status;
			if (status2 != Scp1344Status.Stabbing)
			{
				if (status2 == Scp1344Status.CancelingDeactivation)
				{
					base.OwnerInventory.ServerSelectItem(0);
				}
			}
			else
			{
				this._savedIntensity = 100;
			}
			this.ServerSetStatus(status);
		}

		private void ServerUpdateActive()
		{
			if (this.BlindnessEffect.Intensity <= 15)
			{
				return;
			}
			this._useTime += Time.deltaTime;
			float useTime = this._useTime;
			if (useTime >= 0.75f)
			{
				if (useTime >= 1f)
				{
					float num = 100f - 20f * Mathf.Pow(this._useTime, 0.385f);
					this.BlindnessEffect.Intensity = (byte)num;
					return;
				}
				if (base.IsEquipped)
				{
					base.OwnerInventory.ServerSelectItem(0);
					return;
				}
			}
		}

		private void ServerUpdateDeactivating()
		{
			this._useTime += Time.deltaTime;
			float useTime = this._useTime;
			if (useTime < 0.5f)
			{
				return;
			}
			if (useTime >= 5.1f)
			{
				this.ActivateFinalEffects();
				base.ServerDropItem(true);
				return;
			}
			this.BlindnessEffect.Intensity = this._savedIntensity;
		}

		private void ActivateFinalEffects()
		{
			this.BlindnessEffect.Intensity = 101;
			this.Scp1344Effect.IsEnabled = false;
			this.SeveredEyesEffect.IsEnabled = true;
		}

		private void Awake()
		{
			Scp1344NetworkHandler.OnStatusChanged = (Action<ushort, Scp1344Status>)Delegate.Combine(Scp1344NetworkHandler.OnStatusChanged, new Action<ushort, Scp1344Status>(this.OnStatusChanged));
			DisarmingHandlers.OnPlayerDisarmed += this.OnPlayerDisarmed;
			PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerDied;
		}

		private void OnDestroy()
		{
			Scp1344NetworkHandler.OnStatusChanged = (Action<ushort, Scp1344Status>)Delegate.Remove(Scp1344NetworkHandler.OnStatusChanged, new Action<ushort, Scp1344Status>(this.OnStatusChanged));
			DisarmingHandlers.OnPlayerDisarmed -= this.OnPlayerDisarmed;
			PlayerStats.OnAnyPlayerDied -= this.OnAnyPlayerDied;
		}

		private void OnStatusChanged(ushort serial, Scp1344Status status)
		{
			if (base.ItemSerial != serial)
			{
				return;
			}
			this.Status = status;
			if (status != Scp1344Status.Dropping)
			{
				return;
			}
			this.Scp1344Effect.PlayBuildupSound();
		}

		private void ServerSetStatus(Scp1344Status status)
		{
			Scp1344NetworkHandler.ServerSendMessage(new Scp1344StatusMessage(base.ItemSerial, status));
			if (status != Scp1344Status.Idle)
			{
				if (status == Scp1344Status.Activating)
				{
					base.Owner.EnableWearables(WearableElements.Scp1344Goggles);
					return;
				}
			}
			else
			{
				base.Owner.DisableWearables(WearableElements.Scp1344Goggles);
			}
		}

		private void ClientChangeStatus(Scp1344Status status)
		{
			if (status == Scp1344Status.Deactivating)
			{
				this.IsUsing = false;
				this._useTime = Time.time;
				return;
			}
			if (status != Scp1344Status.Inspecting)
			{
				return;
			}
			this._nextInspectTime = NetworkTime.time + 4.0;
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
				this.IsUsing = false;
				this._useTime = 0f;
				return;
			case Scp1344Status.Active:
				this._useTime = this._cancelationTime;
				this._cancelationTime = 0f;
				this.Scp1344Effect.IsEnabled = true;
				this.BlindnessEffect.Intensity = this._savedIntensity;
				return;
			case Scp1344Status.Dropping:
				this._cancelationTime = this._useTime;
				this._savedIntensity = this.BlindnessEffect.Intensity;
				this.BlindnessEffect.Intensity = 101;
				if (base.OwnerInventory.CurItem.SerialNumber == base.ItemSerial)
				{
					this.ServerSetStatus(Scp1344Status.Deactivating);
					return;
				}
				base.OwnerInventory.ServerSelectItem(base.ItemSerial);
				return;
			case Scp1344Status.CancelingDeactivation:
				this._useTime = 0f;
				this._savedIntensity = this.BlindnessEffect.Intensity;
				this.BlindnessEffect.Intensity = 100;
				return;
			case Scp1344Status.Inspecting:
				this._nextInspectTime = NetworkTime.time + 4.0;
				return;
			default:
				return;
			}
		}

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
	}
}
