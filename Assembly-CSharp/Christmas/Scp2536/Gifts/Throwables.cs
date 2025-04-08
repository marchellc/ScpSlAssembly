using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.ThrowableProjectiles;
using Utils.NonAllocLINQ;

namespace Christmas.Scp2536.Gifts
{
	public class Throwables : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Four;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.GrenadeHE, 40f),
					new Scp2536Reward(ItemType.GrenadeFlash, 45f),
					new Scp2536Reward(ItemType.SCP018, 5f),
					new Scp2536Reward(ItemType.SCP2176, 10f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (base.CanBeGranted(hub))
			{
				return !hub.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> i) => i.Value is ThrowableItem);
			}
			return false;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null);
		}
	}
}
