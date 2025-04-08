using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Mirror;

namespace Scp914.Processors
{
	public class Scp1344ItemProcessor : StandardItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			if (sourcePickup.Info.ItemId != ItemType.SCP1344)
			{
				throw new InvalidOperationException(string.Format("Attempted to use {0} on item with type {1}", "Scp1344ItemProcessor", sourcePickup.Info.ItemId));
			}
			base.ClearCombiner();
			if (setting != Scp914KnobSetting.OneToOne)
			{
				if (setting != Scp914KnobSetting.VeryFine)
				{
					return base.UpgradePickup(setting, sourcePickup);
				}
				this.SpawnItem(ItemType.Adrenaline, sourcePickup);
				this.SpawnItem(ItemType.Adrenaline, sourcePickup);
				this.SpawnItem(ItemType.SCP2176, sourcePickup);
				sourcePickup.DestroySelf();
			}
			else
			{
				this.SpawnItem(ItemType.GrenadeFlash, sourcePickup);
				this.SpawnItem(ItemType.Adrenaline, sourcePickup);
				sourcePickup.DestroySelf();
			}
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
	}
}
