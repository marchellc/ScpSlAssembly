using System;
using System.Collections.Generic;
using InventorySystem.GUI;
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

namespace InventorySystem.Items.MarshmallowMan
{
	public class MarshmallowItem : AutosyncItem, IInteractionBlocker, IHolidayItem
	{
		public static event Action<ushort> OnSwing;

		public static event Action<ushort> OnHit;

		public static event Action<ushort> OnHolsterRequested;

		private UniversalDamageHandler NewDamageHandler
		{
			get
			{
				return new UniversalDamageHandler(this._attackDamage, DeathTranslations.MarshmallowMan, new DamageHandlerBase.CassieAnnouncement
				{
					Announcement = "TERMINATED BY MARSHMALLOW MAN",
					SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.TerminatedByMarshmallowMan, null)
					}
				});
			}
		}

		public override float Weight
		{
			get
			{
				return 0f;
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return false;
			}
		}

		public BlockedInteraction BlockedInteractions
		{
			get
			{
				return BlockedInteraction.OpenInventory | BlockedInteraction.BeDisarmed | BlockedInteraction.GrabItems;
			}
		}

		public bool CanBeCleared
		{
			get
			{
				return !base.IsEquipped;
			}
		}

		public HolidayType[] TargetHolidays { get; } = new HolidayType[] { HolidayType.Christmas };

		public void ServerRequestHolster()
		{
			base.ServerSendPublicRpc(delegate(NetworkWriter writer)
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
			if (!NetworkServer.active)
			{
				return;
			}
			base.OwnerInventory.ServerSelectItem(base.ItemSerial);
			this._marshmallowEffect = base.Owner.playerEffectsController.GetEffect<MarshmallowEffect>();
			this._marshmallowEffect.IsEnabled = true;
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			if (this._markedAsRemoved)
			{
				return;
			}
			this._markedAsRemoved = true;
			if (NetworkServer.active)
			{
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			this._markedAsRemoved = true;
			if (NetworkServer.active)
			{
				this._marshmallowEffect.IsEnabled = false;
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
			{
				Action<ushort> onSwing = MarshmallowItem.OnSwing;
				if (onSwing == null)
				{
					return;
				}
				onSwing(serial);
				return;
			}
			case 1:
			{
				Action<ushort> onHit = MarshmallowItem.OnHit;
				if (onHit == null)
				{
					return;
				}
				onHit(serial);
				return;
			}
			case 2:
			{
				Action<ushort> onHolsterRequested = MarshmallowItem.OnHolsterRequested;
				if (onHolsterRequested == null)
				{
					return;
				}
				onHolsterRequested(serial);
				return;
			}
			default:
				return;
			}
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (reader.ReadByte() == 2)
			{
				this._preventAttacks = true;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.IsLocalPlayer)
			{
				if (!this._cooldown.TolerantIsReady)
				{
					return;
				}
				this._cooldown.Trigger((double)this._attackCooldown);
			}
			RelativePosition relativePosition = reader.ReadRelativePosition();
			Quaternion quaternion = reader.ReadQuaternion();
			ReferenceHub referenceHub;
			FpcBacktracker fpcBacktracker;
			if (reader.Remaining > 0 && reader.TryReadReferenceHub(out referenceHub))
			{
				RelativePosition relativePosition2 = reader.ReadRelativePosition();
				fpcBacktracker = new FpcBacktracker(referenceHub, relativePosition2.Position, 0.4f);
			}
			else
			{
				referenceHub = null;
				fpcBacktracker = null;
			}
			using (new FpcBacktracker(base.Owner, relativePosition.Position, quaternion, 0.1f, 0.15f))
			{
				this.ServerAttack(referenceHub);
			}
			if (fpcBacktracker != null)
			{
				fpcBacktracker.RestorePosition();
			}
			base.ServerSendConditionalRpc((ReferenceHub hub) => hub != base.Owner, delegate(NetworkWriter writer)
			{
				writer.WriteByte(0);
			});
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			if (this.IsLocalPlayer)
			{
				this.UpdateClientInput();
			}
		}

		private void UpdateClientInput()
		{
			if (!this._cooldown.IsReady || this._preventAttacks)
			{
				return;
			}
			if (!InventoryGuiController.ItemsSafeForInteraction)
			{
				return;
			}
			if (!Input.GetKeyDown(NewInput.GetKey(ActionName.Shoot, KeyCode.None)))
			{
				return;
			}
			Action<ushort> onSwing = MarshmallowItem.OnSwing;
			if (onSwing != null)
			{
				onSwing(base.ItemSerial);
			}
			this._cooldown.Trigger((double)this._attackCooldown);
			base.ClientSendCmd(new Action<NetworkWriter>(this.ClientWriteAttackResult));
		}

		private void ClientWriteAttackResult(NetworkWriter writer)
		{
			IFpcRole fpcRole = base.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			writer.WriteRelativePosition(new RelativePosition(fpcRole.FpcModule.Position));
			writer.WriteQuaternion(base.Owner.PlayerCameraReference.rotation);
			this._detectedPlayers.Clear();
			foreach (IDestructible destructible in this.DetectDestructibles())
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if (hitboxIdentity != null)
				{
					this._detectedPlayers.Add(hitboxIdentity.TargetHub);
				}
			}
			ReferenceHub primaryTarget = this._detectedPlayers.GetPrimaryTarget(base.Owner.PlayerCameraReference);
			if (!(primaryTarget == null))
			{
				IFpcRole fpcRole2 = primaryTarget.roleManager.CurrentRole as IFpcRole;
				if (fpcRole2 != null)
				{
					writer.WriteReferenceHub(primaryTarget);
					writer.WriteRelativePosition(new RelativePosition(fpcRole2.FpcModule.Position));
					return;
				}
			}
		}

		private void ServerAttack(ReferenceHub syncTarget)
		{
			foreach (IDestructible destructible in this.DetectDestructibles())
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if ((hitboxIdentity == null || !(hitboxIdentity.TargetHub != syncTarget)) && destructible.Damage(this._attackDamage, this.NewDamageHandler, destructible.CenterOfMass))
				{
					Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
					base.ServerSendPublicRpc(delegate(NetworkWriter writer)
					{
						writer.WriteByte(1);
					});
					break;
				}
			}
		}

		private ArraySegment<IDestructible> DetectDestructibles()
		{
			return ScpAttackAbilityBase<HumanRole>.DetectDestructibles(base.Owner, this._detectionOffset, this._detectionRadius, true);
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

		private readonly TolerantAbilityCooldown _cooldown = new TolerantAbilityCooldown(0.2f);

		private readonly HashSet<ReferenceHub> _detectedPlayers = new HashSet<ReferenceHub>();

		private enum RpcType
		{
			AttackStart,
			Hit,
			Holster
		}
	}
}
