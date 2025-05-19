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
			if (!_charging)
			{
				return !_chargeLoadStopwatch.IsRunning;
			}
			return false;
		}
	}

	public int TotalChargesPerformed { get; private set; }

	public bool MovementOverrideActive => _charging;

	public Vector3 MovementOverrideDirection => base.Owner.transform.forward;

	public bool MovementModifierActive => _charging;

	public float MovementSpeedMultiplier => _chargeMovementSpeedMultiplier;

	public float MovementSpeedLimit => _chargeMovementSpeedLimiter;

	public string Description => ItemTypeId.GetDescription();

	public string Name => ItemTypeId.GetName();

	public AlertContent Alert => _hintHelper.Alert;

	public static event Action<ushort, JailbirdMessageType> OnRpcReceived;

	public event Action<JailbirdMessageType> OnCmdSent;

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		_hitreg.Setup(this);
		_deterioration.Setup(this, _hitreg);
		if (!NetworkServer.active)
		{
			return;
		}
		if (pickup is JailbirdPickup jailbirdPickup)
		{
			TotalChargesPerformed = jailbirdPickup.TotalCharges;
			_hitreg.TotalMeleeDamageDealt = jailbirdPickup.TotalMelee;
			if (!jailbirdPickup.transform.TryGetComponentInParent<Locker>(out var _))
			{
				return;
			}
			base.OwnerInventory.ServerSelectItem(base.ItemSerial);
		}
		_deterioration.RecheckUsage();
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (NetworkServer.active && pickup is JailbirdPickup jailbirdPickup)
		{
			if (_deterioration.IsBroken)
			{
				jailbirdPickup.DestroySelf();
				return;
			}
			jailbirdPickup.TotalCharges = TotalChargesPerformed;
			jailbirdPickup.TotalMelee = _hitreg.TotalMeleeDamageDealt;
			jailbirdPickup.NetworkWear = _deterioration.WearState;
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		_hintHelper.Hide();
		_chargeLoadStopwatch.Reset();
		_charging = false;
		_attackTriggered = false;
		if (NetworkServer.active)
		{
			SendRpc(JailbirdMessageType.Holstered);
			if (_deterioration.IsBroken)
			{
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		_hintHelper.Update(base.Owner);
		if (_deterioration.IsBroken)
		{
			if (NetworkServer.active)
			{
				_brokenRemoveTime -= Time.deltaTime;
			}
			if (_brokenRemoveTime < 0f)
			{
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}
		else if (_charging)
		{
			UpdateCharging();
		}
		else
		{
			if (!base.IsControllable)
			{
				return;
			}
			if (_attackTriggered)
			{
				if (_clientDelayCooldown.IsReady)
				{
					ClientAttack();
					_attackTriggered = false;
				}
				return;
			}
			bool flag = !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			bool flag2 = !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
			if (!_clientAttackCooldown.IsReady)
			{
				return;
			}
			if (GetAction(ActionName.Zoom) && flag)
			{
				if (!_chargeLoadStopwatch.IsRunning)
				{
					SendCmd(JailbirdMessageType.ChargeLoadTriggered);
					_chargeLoadStopwatch.Start();
				}
				if (_chargeLoadStopwatch.Elapsed.TotalSeconds < (double)_chargeAutoengageTime)
				{
					return;
				}
			}
			if (_chargeLoadStopwatch.IsRunning)
			{
				if (_chargeLoadStopwatch.Elapsed.TotalSeconds > (double)_chargeReadyTime && flag)
				{
					SendCmd(JailbirdMessageType.ChargeStarted);
				}
				else
				{
					SendCmd(JailbirdMessageType.ChargeFailed);
				}
				_chargeLoadStopwatch.Reset();
			}
			if (GetActionDown(ActionName.InspectItem) && flag2)
			{
				SendCmd(JailbirdMessageType.Inspect);
			}
			if (GetAction(ActionName.Shoot) && flag)
			{
				_attackTriggered = true;
				SendCmd(JailbirdMessageType.AttackTriggered);
				_clientDelayCooldown.Trigger(_meleeDelay);
				_clientAttackCooldown.Trigger(_meleeCooldown);
			}
		}
	}

	public void ServerReset()
	{
		if (NetworkServer.active)
		{
			_hitreg.TotalMeleeDamageDealt = 0f;
			TotalChargesPerformed = 0;
			_deterioration.RecheckUsage();
		}
	}

	public void ServerOnUpgraded(Scp914KnobSetting setting)
	{
		if (NetworkServer.active)
		{
			if (setting == Scp914KnobSetting.Coarse)
			{
				TotalChargesPerformed = JailbirdDeteriorationTracker.Scp914CoarseCharges;
				_hitreg.TotalMeleeDamageDealt = JailbirdDeteriorationTracker.Scp914CoarseDamage;
				_deterioration.RecheckUsage();
			}
			else
			{
				ServerReset();
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
				AudioSourcePoolManager.PlayOnTransform(_hitClip, hub.transform, 15f);
			}
			break;
		}
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (IsLocalPlayer)
		{
			switch ((JailbirdMessageType)reader.ReadByte())
			{
			case JailbirdMessageType.ChargeStarted:
				_charging = true;
				_firstChargeFrame = true;
				_chargeLoadStopwatch.Reset();
				_chargeAnyDetected = false;
				_chargeResetTime = reader.ReadDouble();
				break;
			case JailbirdMessageType.ChargeFailed:
				_chargeLoadStopwatch.Reset();
				_charging = false;
				break;
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (_deterioration.IsBroken || !base.IsEquipped)
		{
			return;
		}
		JailbirdMessageType jailbirdMessageType = (JailbirdMessageType)reader.ReadByte();
		bool flag = !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
		bool flag2 = !base.Owner.HasBlock(BlockedInteraction.ItemUsage);
		switch (jailbirdMessageType)
		{
		case JailbirdMessageType.AttackTriggered:
			if ((base.IsControllable || !_attackTriggered) && !_chargeLoadStopwatch.IsRunning && _serverAttackCooldown.TolerantIsReady)
			{
				_attackTriggered = true;
				_serverAttackCooldown.Trigger(_meleeCooldown);
				SendRpc(JailbirdMessageType.AttackTriggered);
			}
			break;
		case JailbirdMessageType.AttackPerformed:
			if (_charging)
			{
				ServerAttack(reader);
			}
			else if (_attackTriggered && flag)
			{
				_attackTriggered = false;
				ServerAttack(reader);
			}
			break;
		case JailbirdMessageType.Inspect:
			if (flag2)
			{
				SendRpc(jailbirdMessageType);
			}
			break;
		case JailbirdMessageType.ChargeFailed:
			_chargeLoadStopwatch.Reset();
			SendRpc(jailbirdMessageType);
			break;
		case JailbirdMessageType.ChargeLoadTriggered:
			if (flag && !_charging && !_chargeLoadStopwatch.IsRunning)
			{
				_chargeLoadStopwatch.Start();
				SendRpc(jailbirdMessageType);
			}
			break;
		case JailbirdMessageType.ChargeStarted:
		{
			double totalSeconds = _chargeLoadStopwatch.Elapsed.TotalSeconds;
			if (!flag || totalSeconds - 0.4000000059604645 > (double)_chargeAutoengageTime || totalSeconds + 0.4000000059604645 < (double)_chargeDuration)
			{
				SendRpc(JailbirdMessageType.ChargeFailed);
			}
			else if (!_charging)
			{
				_chargeLoadStopwatch.Reset();
				_charging = true;
				_chargeResetTime = NetworkTime.time + (double)_chargeDuration;
				TotalChargesPerformed++;
				SendRpc(JailbirdMessageType.ChargeStarted, delegate(NetworkWriter wr)
				{
					wr.WriteDouble(_chargeResetTime);
				});
			}
			break;
		}
		}
	}

	private void UpdateCharging()
	{
		double num = _chargeResetTime - NetworkTime.time;
		if (NetworkServer.active && num < -0.4000000059604645)
		{
			ServerAttack(null);
		}
		else
		{
			if (!IsLocalPlayer)
			{
				return;
			}
			if (!_chargeAnyDetected && _hitreg.AnyDetected)
			{
				num = Mathf.Min((float)num, _chargeDetectionDelay);
				_chargeResetTime = NetworkTime.time + num;
				_chargeAnyDetected = true;
			}
			if (num > 0.0)
			{
				if (_firstChargeFrame)
				{
					_firstChargeFrame = false;
					return;
				}
				if (base.Owner.GetVelocity().SqrMagnitudeIgnoreY() > _chargeCancelVelocitySqr)
				{
					return;
				}
			}
			ClientAttack();
			_charging = false;
			_clientAttackCooldown.Trigger(_meleeCooldown);
		}
	}

	private void ServerAttack(NetworkReader reader)
	{
		bool anyDamaged = _hitreg.ServerAttack(_charging, reader);
		if (anyDamaged)
		{
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f);
		}
		SendRpc(JailbirdMessageType.AttackPerformed, delegate(NetworkWriter wr)
		{
			wr.WriteBool(anyDamaged);
		});
		_deterioration.RecheckUsage();
		if (_charging)
		{
			_charging = false;
			if (_deterioration.IsBroken && anyDamaged)
			{
				ExplosionUtils.ServerExplode(base.Owner, ExplosionType.Jailbird);
			}
		}
	}

	private void ClientAttack()
	{
		if (_hitreg.ClientTryAttack())
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
			return _deterioration.WearState == jailbirdItem._deterioration.WearState;
		}
		return false;
	}
}
