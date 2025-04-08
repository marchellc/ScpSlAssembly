using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using UnityEngine;

namespace Scp914.Processors
{
	public class Scp2176ItemProcessor : StandardItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			Scp2176Projectile scp2176Projectile = sourcePickup as Scp2176Projectile;
			if (scp2176Projectile == null)
			{
				throw new InvalidOperationException(string.Format("Attempted to use {0} on item with type {1}", "Scp2176ItemProcessor", sourcePickup.Info.ItemId));
			}
			base.ClearCombiner();
			switch (setting)
			{
			case Scp914KnobSetting.Rough:
				scp2176Projectile.ServerImmediatelyShatter();
				goto IL_00B2;
			case Scp914KnobSetting.OneToOne:
			{
				int num = 0;
				while ((float)num < 12f)
				{
					this.SpawnItem(ItemType.Coin, sourcePickup);
					num++;
				}
				sourcePickup.DestroySelf();
				goto IL_00B2;
			}
			case Scp914KnobSetting.VeryFine:
				if (global::UnityEngine.Random.value >= 0.2f)
				{
					int num2 = 0;
					while ((float)num2 < 1f)
					{
						this.SpawnItem(ItemType.Flashlight, sourcePickup);
						num2++;
					}
					sourcePickup.DestroySelf();
					goto IL_00B2;
				}
				goto IL_00B2;
			}
			return base.UpgradePickup(setting, sourcePickup);
			IL_00B2:
			return base.GenerateResultFromCombiner(sourcePickup);
		}

		private void SpawnItem(ItemType itemType, ItemPickupBase sourcePickup)
		{
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(itemType, out itemBase))
			{
				return;
			}
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo
			{
				ItemId = itemType,
				Serial = ItemSerialGenerator.GenerateNext(),
				WeightKg = itemBase.Weight
			};
			bool flag = NetworkServer.spawned.ContainsKey(sourcePickup.netId);
			ItemPickupBase itemPickupBase = InventoryExtensions.ServerCreatePickup(itemBase, new PickupSyncInfo?(pickupSyncInfo), sourcePickup.Position + Scp914Controller.MoveVector, sourcePickup.Rotation, flag, null);
			base.AddResultToCombiner(itemPickupBase);
		}

		private const float NumOfCoins = 12f;

		private const float NumOfFlashlights = 1f;

		private const float FlashlightChance = 0.2f;
	}
}
