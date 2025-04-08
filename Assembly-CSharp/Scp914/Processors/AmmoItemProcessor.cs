using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace Scp914.Processors
{
	public class AmmoItemProcessor : Scp914ItemProcessor
	{
		public override Scp914Result UpgradeInventoryItem(Scp914KnobSetting setting, ItemBase ib)
		{
			return new Scp914Result(ib, ib, null);
		}

		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			AmmoPickup ammoPickup = sourcePickup as AmmoPickup;
			if (ammoPickup == null)
			{
				throw new InvalidOperationException(string.Format("Attempted to use {0} on item with type {1}", "AmmoItemProcessor", sourcePickup.Info.ItemId));
			}
			ItemType itemType;
			switch (setting)
			{
			case Scp914KnobSetting.Rough:
			case Scp914KnobSetting.Coarse:
				itemType = this._previousAmmo;
				break;
			case Scp914KnobSetting.OneToOne:
				itemType = this._oneToOne;
				break;
			case Scp914KnobSetting.Fine:
			case Scp914KnobSetting.VeryFine:
				itemType = this._nextAmmo;
				break;
			default:
				throw new InvalidOperationException("Undefined knob setting: " + setting.ToString());
			}
			sourcePickup.Position += Scp914Controller.MoveVector;
			int num;
			int num2;
			AmmoItemProcessor.ExchangeAmmo(ammoPickup.Info.ItemId, itemType, (int)ammoPickup.SavedAmmo, out num, out num2);
			if (num2 == 0)
			{
				ammoPickup.DestroySelf();
			}
			else
			{
				ammoPickup.NetworkSavedAmmo = (ushort)num2;
			}
			ItemPickupBase itemPickupBase = AmmoItemProcessor.CreateAmmoPickup(itemType, num, sourcePickup.Position);
			return new Scp914Result(sourcePickup, null, itemPickupBase);
		}

		public static ItemPickupBase CreateAmmoPickup(ItemType type, int bullets, Vector3 pos)
		{
			ItemBase itemBase;
			if (bullets <= 0 || !InventoryItemLoader.AvailableItems.TryGetValue(type, out itemBase))
			{
				return null;
			}
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo
			{
				ItemId = type,
				Serial = ItemSerialGenerator.GenerateNext(),
				WeightKg = itemBase.Weight
			};
			AmmoPickup ammoPickup = InventoryExtensions.ServerCreatePickup(itemBase, new PickupSyncInfo?(pickupSyncInfo), pos, true, null) as AmmoPickup;
			if (ammoPickup != null)
			{
				ammoPickup.NetworkSavedAmmo = (ushort)bullets;
				return ammoPickup;
			}
			return null;
		}

		public static void ExchangeAmmo(ItemType ammoTypeToExchange, ItemType targetAmmoType, int amount, out int exchangedAmmo, out int change)
		{
			AmmoItem ammoItem;
			AmmoItem ammoItem2;
			if (!ammoTypeToExchange.TryGetTemplate(out ammoItem) || !targetAmmoType.TryGetTemplate(out ammoItem2))
			{
				exchangedAmmo = 0;
				change = amount;
				return;
			}
			int unitPrice = ammoItem.UnitPrice;
			int unitPrice2 = ammoItem2.UnitPrice;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int i = 0; i < amount; i++)
			{
				num3 += unitPrice;
				num++;
				if (num3 % unitPrice2 == 0)
				{
					num2 += num3 / unitPrice2;
					num = 0;
					num3 = 0;
				}
			}
			exchangedAmmo = num2;
			change = num;
		}

		[SerializeField]
		private ItemType _previousAmmo;

		[SerializeField]
		private ItemType _oneToOne;

		[SerializeField]
		private ItemType _nextAmmo;
	}
}
