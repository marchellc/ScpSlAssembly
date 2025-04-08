using System;
using Footprinting;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Christmas.Scp2536.Gifts
{
	public class Naughty : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Exclusive;
			}
		}

		internal override bool IgnoredByRandomness
		{
			get
			{
				return true;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.Coal, 65f),
					new Scp2536Reward(ItemType.SpecialCoal, 5f),
					new Scp2536Reward(ItemType.GrenadeFlash, 65f),
					new Scp2536Reward(ItemType.GrenadeHE, 65f),
					new Scp2536Reward(ItemType.SCP018, 65f),
					new Scp2536Reward(ItemType.Coal, 65f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			return global::UnityEngine.Random.Range(0f, 100f) <= 50f && Scp2536GiftController.Gifts.Count((Scp2536GiftBase g) => g.ObtainedBy.Contains(hub)) > 3;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			if (itemType == ItemType.GrenadeFlash || itemType == ItemType.GrenadeHE)
			{
				this.SpawnProjectile(itemType, hub, new Action<ThrownProjectile>(this.SetupEffectGrenade));
				return;
			}
			if (itemType == ItemType.SCP018)
			{
				for (int i = 0; i < 3; i++)
				{
					this.SpawnProjectile(itemType, hub, new Action<ThrownProjectile>(this.SetupScp018));
				}
				return;
			}
			if (itemType != ItemType.None)
			{
				hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
				return;
			}
			Scp956Pinata scp956Pinata;
			if (!Scp956Pinata.TryGetInstance(out scp956Pinata))
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				referenceHub.playerEffectsController.DisableEffect<Scp956Target>();
			}
			Scp956Pinata.ActiveTargets.Clear();
			scp956Pinata.SpawnBehindTarget(hub);
		}

		private void SetupEffectGrenade(ThrownProjectile projectile)
		{
			projectile.ServerActivate();
		}

		private void SetupScp018(ThrownProjectile projectile)
		{
			projectile.GetComponent<Rigidbody>().velocity = global::UnityEngine.Random.onUnitSphere * 15f;
		}

		private void SpawnProjectile(ItemType id, ReferenceHub hub, Action<ThrownProjectile> setupMethod)
		{
			ThrowableItem throwableItem;
			if (!InventoryItemLoader.TryGetItem<ThrowableItem>(id, out throwableItem))
			{
				return;
			}
			ThrownProjectile thrownProjectile = global::UnityEngine.Object.Instantiate<ThrownProjectile>(throwableItem.Projectile, hub.transform.position, Quaternion.identity);
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(id, throwableItem.Weight, ItemSerialGenerator.GenerateNext(), false)
			{
				Locked = true
			};
			thrownProjectile.NetworkInfo = pickupSyncInfo;
			thrownProjectile.PreviousOwner = new Footprint(hub);
			setupMethod(thrownProjectile);
			NetworkServer.Spawn(thrownProjectile.gameObject, null);
		}

		private const float GiftReplacementChance = 50f;

		private const int MaxGiftOpenings = 3;
	}
}
