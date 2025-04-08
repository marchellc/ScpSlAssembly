using System;
using System.Diagnostics;
using AudioPooling;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using Scp914;
using UnityEngine;
using Utils;

namespace InventorySystem.Items.Jailbird
{
	public class JailbirdItem : AutosyncItem, IItemDescription, IItemNametag, IUpgradeTrigger, IUniqueItem, IMovementInputOverride, IMovementSpeedModifier, IItemAlertDrawer, IItemDrawer
	{
		public static event Action<ushort, JailbirdMessageType> OnRpcReceived;

		public override float Weight
		{
			get
			{
				return 1.7f;
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return !this._charging && !this._chargeLoadStopwatch.IsRunning;
			}
		}

		public int TotalChargesPerformed { get; private set; }

		public bool MovementOverrideActive
		{
			get
			{
				return this._charging;
			}
		}

		public Vector3 MovementOverrideDirection
		{
			get
			{
				return base.Owner.transform.forward;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return this._charging;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return this._chargeMovementSpeedMultiplier;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return this._chargeMovementSpeedLimiter;
			}
		}

		public string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public AlertContent Alert
		{
			get
			{
				return this._hintHelper.Alert;
			}
		}

		public event Action<JailbirdMessageType> OnCmdSent;

		public override void OnAdded(ItemPickupBase pickup)
		{
			base.OnAdded(pickup);
			this._hitreg.Setup(this);
			this._deterioration.Setup(this, this._hitreg);
			if (!NetworkServer.active)
			{
				return;
			}
			JailbirdPickup jailbirdPickup = pickup as JailbirdPickup;
			if (jailbirdPickup != null)
			{
				this.TotalChargesPerformed = jailbirdPickup.TotalCharges;
				this._hitreg.TotalMeleeDamageDealt = jailbirdPickup.TotalMelee;
			}
			this._deterioration.RecheckUsage();
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			if (NetworkServer.active)
			{
				JailbirdPickup jailbirdPickup = pickup as JailbirdPickup;
				if (jailbirdPickup != null)
				{
					if (this._deterioration.IsBroken)
					{
						jailbirdPickup.DestroySelf();
						return;
					}
					jailbirdPickup.TotalCharges = this.TotalChargesPerformed;
					jailbirdPickup.TotalMelee = this._hitreg.TotalMeleeDamageDealt;
					jailbirdPickup.NetworkWear = this._deterioration.WearState;
					return;
				}
			}
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			this._hintHelper.Hide();
			this._chargeLoadStopwatch.Reset();
			this._charging = false;
			this._attackTriggered = false;
			if (!NetworkServer.active)
			{
				return;
			}
			this.SendRpc(JailbirdMessageType.Holstered, null);
			if (!this._deterioration.IsBroken)
			{
				return;
			}
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
			{
				this._hintHelper.Update();
			}
			if (this._deterioration.IsBroken)
			{
				if (NetworkServer.active)
				{
					this._brokenRemoveTime -= Time.deltaTime;
				}
				if (this._brokenRemoveTime < 0f)
				{
					base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
				}
				return;
			}
			if (this._charging)
			{
				this.UpdateCharging();
				return;
			}
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (this._attackTriggered)
			{
				if (!this._clientDelayCooldown.IsReady)
				{
					return;
				}
				this.ClientAttack();
				this._attackTriggered = false;
				return;
			}
			else
			{
				bool itemsSafeForInteraction = InventoryGuiController.ItemsSafeForInteraction;
				bool flag = !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
				bool flag2 = !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
				if (!this._clientAttackCooldown.IsReady)
				{
					return;
				}
				if (Input.GetKey(NewInput.GetKey(ActionName.Zoom, KeyCode.None)) && itemsSafeForInteraction && flag)
				{
					if (!this._chargeLoadStopwatch.IsRunning)
					{
						this.SendCmd(JailbirdMessageType.ChargeLoadTriggered);
						this._chargeLoadStopwatch.Start();
					}
					if (this._chargeLoadStopwatch.Elapsed.TotalSeconds < (double)this._chargeAutoengageTime)
					{
						return;
					}
				}
				if (this._chargeLoadStopwatch.IsRunning)
				{
					if (this._chargeLoadStopwatch.Elapsed.TotalSeconds > (double)this._chargeReadyTime && flag)
					{
						this.SendCmd(JailbirdMessageType.ChargeStarted);
					}
					else
					{
						this.SendCmd(JailbirdMessageType.ChargeFailed);
					}
					this._chargeLoadStopwatch.Reset();
				}
				if (!itemsSafeForInteraction)
				{
					return;
				}
				if (Input.GetKeyDown(NewInput.GetKey(ActionName.InspectItem, KeyCode.None)) && flag2)
				{
					this.SendCmd(JailbirdMessageType.Inspect);
				}
				if (!Input.GetKey(NewInput.GetKey(ActionName.Shoot, KeyCode.None)) || !flag)
				{
					return;
				}
				this._attackTriggered = true;
				this.SendCmd(JailbirdMessageType.AttackTriggered);
				this._clientDelayCooldown.Trigger((double)this._meleeDelay);
				this._clientAttackCooldown.Trigger((double)this._meleeCooldown);
				return;
			}
		}

		public void ServerReset()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._hitreg.TotalMeleeDamageDealt = 0f;
			this.TotalChargesPerformed = 0;
			this._deterioration.RecheckUsage();
		}

		public void ServerOnUpgraded(Scp914KnobSetting setting)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (setting == Scp914KnobSetting.Coarse)
			{
				this.TotalChargesPerformed = JailbirdDeteriorationTracker.Scp914CoarseCharges;
				this._hitreg.TotalMeleeDamageDealt = JailbirdDeteriorationTracker.Scp914CoarseDamage;
				this._deterioration.RecheckUsage();
				return;
			}
			this.ServerReset();
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			JailbirdMessageType jailbirdMessageType = (JailbirdMessageType)reader.ReadByte();
			Action<ushort, JailbirdMessageType> onRpcReceived = JailbirdItem.OnRpcReceived;
			if (onRpcReceived != null)
			{
				onRpcReceived(serial, jailbirdMessageType);
			}
			if (jailbirdMessageType == JailbirdMessageType.UpdateState)
			{
				JailbirdDeteriorationTracker.ReadUsage(serial, reader);
				return;
			}
			if (jailbirdMessageType != JailbirdMessageType.AttackPerformed)
			{
				return;
			}
			if (!reader.ReadBool())
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
			{
				return;
			}
			AudioSourcePoolManager.PlayOnTransform(this._hitClip, referenceHub.transform, 15f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (!this.IsLocalPlayer)
			{
				return;
			}
			JailbirdMessageType jailbirdMessageType = (JailbirdMessageType)reader.ReadByte();
			if (jailbirdMessageType != JailbirdMessageType.ChargeFailed)
			{
				if (jailbirdMessageType == JailbirdMessageType.ChargeStarted)
				{
					this._charging = true;
					this._firstChargeFrame = true;
					this._chargeLoadStopwatch.Reset();
					this._chargeAnyDetected = false;
					this._chargeResetTime = reader.ReadDouble();
					return;
				}
			}
			else
			{
				this._chargeLoadStopwatch.Reset();
				this._charging = false;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (this._deterioration.IsBroken || !base.IsEquipped)
			{
				return;
			}
			JailbirdMessageType jailbirdMessageType = (JailbirdMessageType)reader.ReadByte();
			bool flag = !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			bool flag2 = !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
			switch (jailbirdMessageType)
			{
			case JailbirdMessageType.AttackTriggered:
				if (this._chargeLoadStopwatch.IsRunning || this._attackTriggered || !this._serverAttackCooldown.TolerantIsReady)
				{
					return;
				}
				this._attackTriggered = true;
				this._serverAttackCooldown.Trigger((double)this._meleeCooldown);
				this.SendRpc(JailbirdMessageType.AttackTriggered, null);
				return;
			case JailbirdMessageType.AttackPerformed:
				if (this._charging)
				{
					this.ServerAttack(reader);
					return;
				}
				if (!this._attackTriggered || !flag)
				{
					return;
				}
				this._attackTriggered = false;
				this.ServerAttack(reader);
				return;
			case JailbirdMessageType.ChargeLoadTriggered:
				if (flag)
				{
					if (this._charging || this._chargeLoadStopwatch.IsRunning)
					{
						return;
					}
					this._chargeLoadStopwatch.Start();
					this.SendRpc(jailbirdMessageType, null);
					return;
				}
				break;
			case JailbirdMessageType.ChargeFailed:
				this._chargeLoadStopwatch.Reset();
				this.SendRpc(jailbirdMessageType, null);
				return;
			case JailbirdMessageType.ChargeStarted:
			{
				double totalSeconds = this._chargeLoadStopwatch.Elapsed.TotalSeconds;
				if (!flag || totalSeconds - 0.4000000059604645 > (double)this._chargeAutoengageTime || totalSeconds + 0.4000000059604645 < (double)this._chargeDuration)
				{
					this.SendRpc(JailbirdMessageType.ChargeFailed, null);
					return;
				}
				if (this._charging)
				{
					return;
				}
				this._chargeLoadStopwatch.Reset();
				this._charging = true;
				this._chargeResetTime = NetworkTime.time + (double)this._chargeDuration;
				int totalChargesPerformed = this.TotalChargesPerformed;
				this.TotalChargesPerformed = totalChargesPerformed + 1;
				this.SendRpc(JailbirdMessageType.ChargeStarted, delegate(NetworkWriter wr)
				{
					wr.WriteDouble(this._chargeResetTime);
				});
				break;
			}
			case JailbirdMessageType.Inspect:
				if (flag2)
				{
					this.SendRpc(jailbirdMessageType, null);
					return;
				}
				break;
			default:
				return;
			}
		}

		private void UpdateCharging()
		{
			double num = this._chargeResetTime - NetworkTime.time;
			if (NetworkServer.active && num < -0.4000000059604645)
			{
				this.ServerAttack(null);
				return;
			}
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (!this._chargeAnyDetected && this._hitreg.AnyDetected)
			{
				num = (double)Mathf.Min((float)num, this._chargeDetectionDelay);
				this._chargeResetTime = NetworkTime.time + num;
				this._chargeAnyDetected = true;
			}
			if (num > 0.0)
			{
				if (this._firstChargeFrame)
				{
					this._firstChargeFrame = false;
					return;
				}
				if (base.Owner.GetVelocity().SqrMagnitudeIgnoreY() > this._chargeCancelVelocitySqr)
				{
					return;
				}
			}
			this.ClientAttack();
			this._charging = false;
			this._clientAttackCooldown.Trigger((double)this._meleeCooldown);
		}

		private void ServerAttack(NetworkReader reader)
		{
			bool anyDamaged = this._hitreg.ServerAttack(this._charging, reader);
			if (anyDamaged)
			{
				Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
			}
			this.SendRpc(JailbirdMessageType.AttackPerformed, delegate(NetworkWriter wr)
			{
				wr.WriteBool(anyDamaged);
			});
			this._deterioration.RecheckUsage();
			if (!this._charging)
			{
				return;
			}
			this._charging = false;
			if (!this._deterioration.IsBroken || !anyDamaged)
			{
				return;
			}
			ExplosionUtils.ServerExplode(base.Owner, ExplosionType.Jailbird);
		}

		private void ClientAttack()
		{
			if (!this._hitreg.ClientTryAttack())
			{
				return;
			}
			Action<JailbirdMessageType> onCmdSent = this.OnCmdSent;
			if (onCmdSent == null)
			{
				return;
			}
			onCmdSent(JailbirdMessageType.AttackPerformed);
		}

		private void SendRpc(JailbirdMessageType header, Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, out networkWriter))
			{
				networkWriter.WriteByte((byte)header);
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		private void SendCmd(JailbirdMessageType msg)
		{
			NetworkWriter networkWriter;
			using (new AutosyncCmd(base.ItemId, out networkWriter))
			{
				networkWriter.WriteByte((byte)msg);
			}
			Action<JailbirdMessageType> onCmdSent = this.OnCmdSent;
			if (onCmdSent == null)
			{
				return;
			}
			onCmdSent(msg);
		}

		public bool CompareIdentical(ItemBase other)
		{
			JailbirdItem jailbirdItem = other as JailbirdItem;
			return jailbirdItem != null && this._deterioration.WearState == jailbirdItem._deterioration.WearState;
		}

		private const ActionName TriggerMelee = ActionName.Shoot;

		private const ActionName TriggerCharge = ActionName.Zoom;

		private const ActionName InspectKey = ActionName.InspectItem;

		private const float ServerChargeTolerance = 0.4f;

		private double _chargeResetTime;

		private bool _charging;

		private readonly Stopwatch _chargeLoadStopwatch = new Stopwatch();

		private bool _chargeAnyDetected;

		private bool _firstChargeFrame;

		private bool _attackTriggered;

		private readonly TolerantAbilityCooldown _serverAttackCooldown = new TolerantAbilityCooldown(0.2f);

		private readonly AbilityCooldown _clientAttackCooldown = new AbilityCooldown();

		private readonly AbilityCooldown _clientDelayCooldown = new AbilityCooldown();

		private readonly ItemHintAlertHelper _hintHelper = new ItemHintAlertHelper(InventoryGuiTranslation.JailbirdAttackHint, new ActionName?(ActionName.Shoot), InventoryGuiTranslation.JailbirdChargeHint, new ActionName?(ActionName.Zoom), 0.3f, 1f, 8f);

		[SerializeField]
		private AudioClip _hitClip;

		[SerializeField]
		private float _meleeDelay;

		[SerializeField]
		private float _meleeCooldown;

		[SerializeField]
		private float _chargeDuration;

		[SerializeField]
		private float _chargeReadyTime;

		[SerializeField]
		private float _chargeMovementSpeedMultiplier;

		[SerializeField]
		private float _chargeMovementSpeedLimiter;

		[SerializeField]
		private float _chargeCancelVelocitySqr;

		[SerializeField]
		private float _chargeAutoengageTime;

		[SerializeField]
		private float _chargeDetectionDelay;

		[SerializeField]
		private float _brokenRemoveTime;

		[SerializeField]
		private JailbirdHitreg _hitreg;

		[SerializeField]
		private JailbirdDeteriorationTracker _deterioration;
	}
}
