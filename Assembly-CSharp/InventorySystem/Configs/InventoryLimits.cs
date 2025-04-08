using System;
using System.Collections.Generic;
using InventorySystem.Items.Armor;
using UnityEngine;

namespace InventorySystem.Configs
{
	public static class InventoryLimits
	{
		private static ServerConfigSynchronizer Config
		{
			get
			{
				return ServerConfigSynchronizer.Singleton;
			}
		}

		public static ushort GetAmmoLimit(ItemType ammoType, ReferenceHub player)
		{
			BodyArmor bodyArmor;
			return InventoryLimits.GetAmmoLimit((player != null && player.inventory.TryGetBodyArmor(out bodyArmor)) ? bodyArmor : null, ammoType);
		}

		public static sbyte GetCategoryLimit(ItemCategory category, ReferenceHub player)
		{
			BodyArmor bodyArmor;
			return InventoryLimits.GetCategoryLimit((player != null && player.inventory.TryGetBodyArmor(out bodyArmor)) ? bodyArmor : null, category);
		}

		public static ushort GetAmmoLimit(BodyArmor armor, ItemType ammoType)
		{
			int num = -1;
			foreach (ServerConfigSynchronizer.AmmoLimit ammoLimit in InventoryLimits.Config.AmmoLimitsSync)
			{
				if (ammoLimit.AmmoType == ammoType)
				{
					if (ammoLimit.Limit == 0)
					{
						return ushort.MaxValue;
					}
					num = (int)ammoLimit.Limit;
					break;
				}
			}
			if (num == -1)
			{
				ushort num2;
				if (!InventoryLimits.StandardAmmoLimits.TryGetValue(ammoType, out num2))
				{
					return ushort.MaxValue;
				}
				num = (int)num2;
			}
			if (armor != null)
			{
				foreach (BodyArmor.ArmorAmmoLimit armorAmmoLimit in armor.AmmoLimits)
				{
					if (armorAmmoLimit.AmmoType == ammoType)
					{
						num += (int)armorAmmoLimit.Limit;
						break;
					}
				}
			}
			return (ushort)Mathf.Min(65535, num);
		}

		public static sbyte GetCategoryLimit(BodyArmor armor, ItemCategory category)
		{
			int num = InventoryLimits.Config.CategoryLimits.Count;
			int num2 = 0;
			int num3 = 0;
			while (Enum.IsDefined(typeof(ItemCategory), (ItemCategory)num2))
			{
				ItemCategory itemCategory = (ItemCategory)num2;
				if (itemCategory == category)
				{
					num = num3;
					break;
				}
				sbyte b;
				if (InventoryLimits.StandardCategoryLimits.TryGetValue(itemCategory, out b) && b >= 0)
				{
					num3++;
				}
				num2++;
			}
			int num4;
			if (num < InventoryLimits.Config.CategoryLimits.Count)
			{
				num4 = (int)InventoryLimits.Config.CategoryLimits[num];
			}
			else
			{
				sbyte b2;
				if (!InventoryLimits.StandardCategoryLimits.TryGetValue(category, out b2))
				{
					return 8;
				}
				num4 = (int)b2;
			}
			if (armor != null)
			{
				foreach (BodyArmor.ArmorCategoryLimitModifier armorCategoryLimitModifier in armor.CategoryLimits)
				{
					if (armorCategoryLimitModifier.Category == category)
					{
						num4 += (int)armorCategoryLimitModifier.Limit;
						break;
					}
				}
			}
			return (sbyte)Mathf.Clamp(num4, -8, 8);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static InventoryLimits()
		{
			Dictionary<ItemType, ushort> dictionary = new Dictionary<ItemType, ushort>();
			dictionary[ItemType.Ammo9x19] = 40;
			dictionary[ItemType.Ammo556x45] = 40;
			dictionary[ItemType.Ammo762x39] = 40;
			dictionary[ItemType.Ammo44cal] = 18;
			dictionary[ItemType.Ammo12gauge] = 14;
			InventoryLimits.StandardAmmoLimits = dictionary;
			Dictionary<ItemCategory, sbyte> dictionary2 = new Dictionary<ItemCategory, sbyte>();
			dictionary2[ItemCategory.Armor] = -1;
			dictionary2[ItemCategory.Grenade] = 2;
			dictionary2[ItemCategory.Keycard] = 3;
			dictionary2[ItemCategory.Medical] = 3;
			dictionary2[ItemCategory.SpecialWeapon] = -1;
			dictionary2[ItemCategory.Radio] = -1;
			dictionary2[ItemCategory.SCPItem] = 3;
			dictionary2[ItemCategory.Firearm] = 1;
			InventoryLimits.StandardCategoryLimits = dictionary2;
		}

		public static readonly Dictionary<ItemType, ushort> StandardAmmoLimits;

		public static readonly Dictionary<ItemCategory, sbyte> StandardCategoryLimits;
	}
}
