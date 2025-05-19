using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using Subtitles;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.MarshmallowMan;

public class MarshmallowItem : AutosyncItem, IInteractionBlocker, IHolidayItem
{
	private enum RpcType
	{
		AttackStart,
		Hit,
		Holster
	}

	public const float HolsterAnimTime = 1.1f;

	[SerializeField]
	private float _detectionRadius;

	[SerializeField]
	private float _detectionOffset;

	[SerializeField]
	private float _attackCooldown;

	[SerializeField]
	private float _attackDamage;

	private bool _markedAsRemoved;

	private bool _preventAttacks;

	private MarshmallowEffect _marshmallowEffect;

	private readonly TolerantAbilityCooldown _cooldown = new TolerantAbilityCooldown();

	private readonly HashSet<ReferenceHub> _detectedPlayers = new HashSet<ReferenceHub>();

	private UniversalDamageHandler NewDamageHandler => new UniversalDamageHandler(_attackDamage, DeathTranslations.MarshmallowMan, new DamageHandlerBase.CassieAnnouncement
	{
		Announcement = "TERMINATED BY MARSHMALLOW MAN",
		SubtitleParts = new SubtitlePart[1]
		{
			new SubtitlePart(SubtitleType.TerminatedByMarshmallowMan, (string[])null)
		}
	});

	public override float Weight => 0f;

	public override bool AllowHolster => false;

	public BlockedInteraction BlockedInteractions => BlockedInteraction.OpenInventory | BlockedInteraction.BeDisarmed | BlockedInteraction.GrabItems;

	public bool CanBeCleared => !base.IsEquipped;

	public HolidayType[] TargetHolidays { get; } = new HolidayType[1] { HolidayType.Christmas };

	public static event Action<ushort> OnSwing;

	public static event Action<ushort> OnHit;

	public static event Action<ushort> OnHolsterRequested;

	public void ServerRequestHolster()
	{
		ServerSendPublicRpc(delegate(NetworkWriter writer)
		{
			writer.WriteByte(2);
		});
	}

	public override ItemPickupBase ServerDropItem(bool spawn)
	{
		base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		return null;
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		base.Owner.interCoordinator.AddBlocker(this);
		if (NetworkServer.active)
		{
			base.OwnerInventory.ServerSelectItem(base.ItemSerial);
			_marshmallowEffect = base.Owner.playerEffectsController.GetEffect<MarshmallowEffect>();
			_marshmallowEffect.IsEnabled = true;
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (!_markedAsRemoved)
		{
			_markedAsRemoved = true;
			if (NetworkServer.active)
			{
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		_markedAsRemoved = true;
		if (NetworkServer.active)
		{
			_marshmallowEffect.IsEnabled = false;
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.Hit:
			MarshmallowItem.OnHit?.Invoke(serial);
			break;
		case RpcType.AttackStart:
			MarshmallowItem.OnSwing?.Invoke(serial);
			break;
		case RpcType.Holster:
			MarshmallowItem.OnHolsterRequested?.Invoke(serial);
			break;
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (IsLocalPlayer && reader.ReadByte() == 2)
		{
			_preventAttacks = true;
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (!IsLocalPlayer)
		{
			if (!_cooldown.TolerantIsReady)
			{
				return;
			}
			_cooldown.Trigger(_attackCooldown);
		}
		RelativePosition relativePosition = reader.ReadRelativePosition();
		Quaternion claimedRot = reader.ReadQuaternion();
		FpcBacktracker fpcBacktracker;
		if (reader.Remaining > 0 && reader.TryReadReferenceHub(out var hub2))
		{
			RelativePosition relativePosition2 = reader.ReadRelativePosition();
			fpcBacktracker = new FpcBacktracker(hub2, relativePosition2.Position);
		}
		else
		{
			hub2 = null;
			fpcBacktracker = null;
		}
		using (new FpcBacktracker(base.Owner, relativePosition.Position, claimedRot))
		{
			ServerAttack(hub2);
		}
		fpcBacktracker?.RestorePosition();
		ServerSendConditionalRpc((ReferenceHub hub) => hub != base.Owner, delegate(NetworkWriter writer)
		{
			writer.WriteByte(0);
		});
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsControllable)
		{
			UpdateClientInput();
		}
	}

	private void UpdateClientInput()
	{
		if (_cooldown.IsReady && !_preventAttacks && GetActionDown(ActionName.Shoot))
		{
			MarshmallowItem.OnSwing?.Invoke(base.ItemSerial);
			_cooldown.Trigger(_attackCooldown);
			ClientSendCmd(ClientWriteAttackResult);
		}
	}

	private void ClientWriteAttackResult(NetworkWriter writer)
	{
		if (!(base.Owner.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return;
		}
		writer.WriteRelativePosition(new RelativePosition(fpcRole.FpcModule.Position));
		writer.WriteQuaternion(base.Owner.PlayerCameraReference.rotation);
		_detectedPlayers.Clear();
		foreach (IDestructible item in DetectDestructibles())
		{
			if (item is HitboxIdentity hitboxIdentity)
			{
				_detectedPlayers.Add(hitboxIdentity.TargetHub);
			}
		}
		ReferenceHub primaryTarget = _detectedPlayers.GetPrimaryTarget(base.Owner.PlayerCameraReference);
		if (!(primaryTarget == null) && primaryTarget.roleManager.CurrentRole is IFpcRole fpcRole2)
		{
			writer.WriteReferenceHub(primaryTarget);
			writer.WriteRelativePosition(new RelativePosition(fpcRole2.FpcModule.Position));
		}
	}

	private void ServerAttack(ReferenceHub syncTarget)
	{
		foreach (IDestructible item in DetectDestructibles())
		{
			if ((!(item is HitboxIdentity hitboxIdentity) || !(hitboxIdentity.TargetHub != syncTarget)) && item.Damage(_attackDamage, NewDamageHandler, item.CenterOfMass))
			{
				Hitmarker.SendHitmarkerDirectly(base.Owner, 1f);
				ServerSendPublicRpc(delegate(NetworkWriter writer)
				{
					writer.WriteByte(1);
				});
				break;
			}
		}
	}

	private ArraySegment<IDestructible> DetectDestructibles()
	{
		return ScpAttackAbilityBase<HumanRole>.DetectDestructibles(base.Owner, _detectionOffset, _detectionRadius);
	}
}
