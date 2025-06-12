using System;
using System.Diagnostics;
using AudioPooling;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using Scp914;
using UnityEngine;
using Utils;

namespace InventorySystem.Items.Jailbird;

public class JailbirdItem : AutosyncItem, IItemDescription, IItemNametag, IUpgradeTrigger, IUniqueItem, IMovementInputOverride, IMovementSpeedModifier, IItemAlertDrawer, IItemDrawer
{
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

	private readonly TolerantAbilityCooldown _serverAttackCooldown = new TolerantAbilityCooldown();

	private readonly AbilityCooldown _clientAttackCooldown = new AbilityCooldown();

	private readonly AbilityCooldown _clientDelayCooldown = new AbilityCooldown();

	private readonly ItemHintAlertHelper _hintHelper = new ItemHintAlertHelper(InventoryGuiTranslation.JailbirdAttackHint, ActionName.Shoot, InventoryGuiTranslation.JailbirdChargeHint, ActionName.Zoom);

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

	public override float Weight => 1.7f;

	public override bool AllowHolster
	{
		get
		{
			if (!this._charging)
			{
				return !this._chargeLoadStopwatch.IsRunning;
			}
			return false;
		}
	}

	public int TotalChargesPerformed { get; private set; }

	public bool MovementOverrideActive => this._charging;

	public Vector3 MovementOverrideDirection => base.Owner.transform.forward;

	public bool MovementModifierActive => this._charging;

	public float MovementSpeedMultiplier => this._chargeMovementSpeedMultiplier;

	public float MovementSpeedLimit => this._chargeMovementSpeedLimiter;

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	public AlertContent Alert => this._hintHelper.Alert;

	public static event Action<ushort, JailbirdMessageType> OnRpcReceived;

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
		if (pickup is JailbirdPickup jailbirdPickup)
		{
			this.TotalChargesPerformed = jailbirdPickup.TotalCharges;
			this._hitreg.TotalMeleeDamageDealt = jailbirdPickup.TotalMelee;
			if (!jailbirdPickup.transform.TryGetComponentInParent<Locker>(out var _))
			{
				return;
			}
			base.OwnerInventory.ServerSelectItem(base.ItemSerial);
		}
		this._deterioration.RecheckUsage();
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (NetworkServer.active && pickup is JailbirdPickup jailbirdPickup)
		{
			if (this._deterioration.IsBroken)
			{
				jailbirdPickup.DestroySelf();
				return;
			}
			jailbirdPickup.TotalCharges = this.TotalChargesPerformed;
			jailbirdPickup.TotalMelee = this._hitreg.TotalMeleeDamageDealt;
			jailbirdPickup.NetworkWear = this._deterioration.WearState;
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		this._hintHelper.Hide();
		this._chargeLoadStopwatch.Reset();
		this._charging = false;
		this._attackTriggered = false;
		if (NetworkServer.active)
		{
			this.SendRpc(JailbirdMessageType.Holstered);
			if (this._deterioration.IsBroken)
			{
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		this._hintHelper.Update(base.Owner);
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
		}
		else if (this._charging)
		{
			this.UpdateCharging();
		}
		else
		{
			if (!base.IsControllable)
			{
				return;
			}
			if (this._attackTriggered)
			{
				if (this._clientDelayCooldown.IsReady)
				{
					this.ClientAttack();
					this._attackTriggered = false;
				}
				return;
			}
			bool flag = !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			bool flag2 = !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
			if (!this._clientAttackCooldown.IsReady)
			{
				return;
			}
			if (base.GetAction(ActionName.Zoom) && flag)
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
			if (base.GetActionDown(ActionName.InspectItem) && flag2)
			{
				this.SendCmd(JailbirdMessageType.Inspect);
			}
			if (base.GetAction(ActionName.Shoot) && flag)
			{
				this._attackTriggered = true;
				this.SendCmd(JailbirdMessageType.AttackTriggered);
				this._clientDelayCooldown.Trigger(this._meleeDelay);
				this._clientAttackCooldown.Trigger(this._meleeCooldown);
			}
		}
	}

	public void ServerReset()
	{
		if (NetworkServer.active)
		{
			this._hitreg.TotalMeleeDamageDealt = 0f;
			this.TotalChargesPerformed = 0;
			this._deterioration.RecheckUsage();
		}
	}

	public void ServerOnUpgraded(Scp914KnobSetting setting)
	{
		if (NetworkServer.active)
		{
			if (setting == Scp914KnobSetting.Coarse)
			{
				this.TotalChargesPerformed = JailbirdDeteriorationTracker.Scp914CoarseCharges;
				this._hitreg.TotalMeleeDamageDealt = JailbirdDeteriorationTracker.Scp914CoarseDamage;
				this._deterioration.RecheckUsage();
			}
			else
			{
				this.ServerReset();
			}
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		JailbirdMessageType jailbirdMessageType = (JailbirdMessageType)reader.ReadByte();
		JailbirdItem.OnRpcReceived?.Invoke(serial, jailbirdMessageType);
		switch (jailbirdMessageType)
		{
		case JailbirdMessageType.UpdateState:
			JailbirdDeteriorationTracker.ReadUsage(serial, reader);
			break;
		case JailbirdMessageType.AttackPerformed:
		{
			if (reader.ReadBool() && InventoryExtensions.TryGetHubHoldingSerial(serial, out var hub))
			{
				AudioSourcePoolManager.PlayOnTransform(this._hitClip, hub.transform, 15f);
			}
			break;
		}
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (this.IsLocalPlayer)
		{
			switch ((JailbirdMessageType)reader.ReadByte())
			{
			case JailbirdMessageType.ChargeStarted:
				this._charging = true;
				this._firstChargeFrame = true;
				this._chargeLoadStopwatch.Reset();
				this._chargeAnyDetected = false;
				this._chargeResetTime = reader.ReadDouble();
				break;
			case JailbirdMessageType.ChargeFailed:
				this._chargeLoadStopwatch.Reset();
				this._charging = false;
				break;
			}
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
			if ((base.IsControllable || !this._attackTriggered) && !this._chargeLoadStopwatch.IsRunning && this._serverAttackCooldown.TolerantIsReady)
			{
				this._attackTriggered = true;
				this._serverAttackCooldown.Trigger(this._meleeCooldown);
				this.SendRpc(JailbirdMessageType.AttackTriggered);
			}
			break;
		case JailbirdMessageType.AttackPerformed:
			if (this._charging)
			{
				this.ServerAttack(reader);
			}
			else if (this._attackTriggered && flag)
			{
				this._attackTriggered = false;
				this.ServerAttack(reader);
			}
			break;
		case JailbirdMessageType.Inspect:
			if (flag2)
			{
				this.SendRpc(jailbirdMessageType);
			}
			break;
		case JailbirdMessageType.ChargeFailed:
			this._chargeLoadStopwatch.Reset();
			this.SendRpc(jailbirdMessageType);
			break;
		case JailbirdMessageType.ChargeLoadTriggered:
			if (flag && !this._charging && !this._chargeLoadStopwatch.IsRunning)
			{
				this._chargeLoadStopwatch.Start();
				this.SendRpc(jailbirdMessageType);
			}
			break;
		case JailbirdMessageType.ChargeStarted:
		{
			double totalSeconds = this._chargeLoadStopwatch.Elapsed.TotalSeconds;
			if (!flag || totalSeconds - 0.4000000059604645 > (double)this._chargeAutoengageTime || totalSeconds + 0.4000000059604645 < (double)this._chargeDuration)
			{
				this.SendRpc(JailbirdMessageType.ChargeFailed);
			}
			else if (!this._charging)
			{
				this._chargeLoadStopwatch.Reset();
				this._charging = true;
				this._chargeResetTime = NetworkTime.time + (double)this._chargeDuration;
				this.TotalChargesPerformed++;
				this.SendRpc(JailbirdMessageType.ChargeStarted, delegate(NetworkWriter wr)
				{
					wr.WriteDouble(this._chargeResetTime);
				});
			}
			break;
		}
		}
	}

	private void UpdateCharging()
	{
		double num = this._chargeResetTime - NetworkTime.time;
		if (NetworkServer.active && num < -0.4000000059604645)
		{
			this.ServerAttack(null);
		}
		else
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (!this._chargeAnyDetected && this._hitreg.AnyDetected)
			{
				num = Mathf.Min((float)num, this._chargeDetectionDelay);
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
			this._clientAttackCooldown.Trigger(this._meleeCooldown);
		}
	}

	private void ServerAttack(NetworkReader reader)
	{
		bool anyDamaged = this._hitreg.ServerAttack(this._charging, reader);
		if (anyDamaged)
		{
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f);
		}
		this.SendRpc(JailbirdMessageType.AttackPerformed, delegate(NetworkWriter wr)
		{
			wr.WriteBool(anyDamaged);
		});
		this._deterioration.RecheckUsage();
		if (this._charging)
		{
			this._charging = false;
			if (this._deterioration.IsBroken && anyDamaged)
			{
				ExplosionUtils.ServerExplode(base.Owner, ExplosionType.Jailbird);
			}
		}
	}

	private void ClientAttack()
	{
		if (this._hitreg.ClientTryAttack())
		{
			this.OnCmdSent?.Invoke(JailbirdMessageType.AttackPerformed);
		}
	}

	private void SendRpc(JailbirdMessageType header, Action<NetworkWriter> extraData = null)
	{
		NetworkWriter writer;
		using (new AutosyncRpc(base.ItemId, out writer))
		{
			writer.WriteByte((byte)header);
			extraData?.Invoke(writer);
		}
	}

	private void SendCmd(JailbirdMessageType msg)
	{
		NetworkWriter writer;
		using (new AutosyncCmd(base.ItemId, out writer))
		{
			writer.WriteByte((byte)msg);
		}
		this.OnCmdSent?.Invoke(msg);
	}

	public bool CompareIdentical(ItemBase other)
	{
		if (other is JailbirdItem jailbirdItem)
		{
			return this._deterioration.WearState == jailbirdItem._deterioration.WearState;
		}
		return false;
	}
}
