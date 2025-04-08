using System;
using System.Collections.Generic;

namespace InventorySystem
{
	public readonly struct InventoryRoleInfo
	{
		public InventoryRoleInfo(ItemType[] items, Dictionary<ItemType, ushort> ammo)
		{
			this.Items = items;
			this.Ammo = ammo;
		}

		public readonly ItemType[] Items;

		public readonly Dictionary<ItemType, ushort> Ammo;
	}
}
