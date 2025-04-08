using System;
using System.Collections.Generic;
using InventorySystem.Configs;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem.Items.Armor
{
	public static class BodyArmorUtils
	{
		public static bool TryGetBodyArmor(this Inventory inv, out BodyArmor bodyArmor)
		{
			ushort num;
			return inv.TryGetBodyArmorAndItsSerial(out bodyArmor, out num);
		}

		public static bool TryGetBodyArmorAndItsSerial(this Inventory inv, out BodyArmor bodyArmor, out ushort serial)
		{
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in inv.UserInventory.Items)
			{
				BodyArmor bodyArmor2 = keyValuePair.Value as BodyArmor;
				if (bodyArmor2 != null)
				{
					serial = keyValuePair.Key;
					bodyArmor = bodyArmor2;
					return true;
				}
			}
			serial = 0;
			bodyArmor = null;
			return false;
		}

		public static float ProcessDamage(int efficacy, float baseDamage, int bulletPenetrationPercent)
		{
			float num = (float)efficacy / 100f;
			float num2 = (float)bulletPenetrationPercent / 100f;
			return baseDamage * (1f - num * (1f - num2));
		}

		public static void RemoveEverythingExceedingLimits(this Inventory inv, bool removeItems = true, bool removeAmmo = true)
		{
			BodyArmor bodyArmor;
			inv.TryGetBodyArmor(out bodyArmor);
			inv.RemoveEverythingExceedingLimits(bodyArmor, removeItems, removeAmmo);
		}

		public static void RemoveEverythingExceedingLimits(this Inventory inv, BodyArmor armor, bool removeItems = true, bool removeAmmo = true)
		{
			HashSet<ushort> hashSet = HashSetPool<ushort>.Shared.Rent();
			Dictionary<ItemCategory, int> dictionary = new Dictionary<ItemCategory, int>();
			Dictionary<ItemType, ushort> dictionary2 = new Dictionary<ItemType, ushort>();
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in inv.UserInventory.Items)
			{
				if (keyValuePair.Value.Category != ItemCategory.Armor)
				{
					int num = Mathf.Abs((int)InventoryLimits.GetCategoryLimit(armor, keyValuePair.Value.Category));
					int num2;
					if (!dictionary.TryGetValue(keyValuePair.Value.Category, out num2))
					{
						num2 = 1;
					}
					else
					{
						num2++;
					}
					if (num2 > num)
					{
						hashSet.Add(keyValuePair.Key);
					}
					dictionary[keyValuePair.Value.Category] = num2;
				}
			}
			foreach (KeyValuePair<ItemType, ushort> keyValuePair2 in inv.UserInventory.ReserveAmmo)
			{
				ushort ammoLimit = InventoryLimits.GetAmmoLimit(armor, keyValuePair2.Key);
				if (keyValuePair2.Value > ammoLimit)
				{
					dictionary2.Add(keyValuePair2.Key, keyValuePair2.Value - ammoLimit);
				}
			}
			if (removeItems)
			{
				foreach (ushort num3 in hashSet)
				{
					inv.ServerDropItem(num3);
				}
			}
			if (removeAmmo)
			{
				foreach (KeyValuePair<ItemType, ushort> keyValuePair3 in dictionary2)
				{
					inv.ServerDropAmmo(keyValuePair3.Key, keyValuePair3.Value, false);
				}
			}
		}
	}
}
