using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;

namespace Christmas.Scp2536.Gifts
{
	public class TierFourWeapons : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Three;
			}
		}

		public override float SecondsUntilAvailable
		{
			get
			{
				return 600f;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.GunLogicer, 50f),
					new Scp2536Reward(ItemType.GunFRMG0, 50f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (!base.CanBeGranted(hub))
			{
				return false;
			}
			int num = 0;
			using (Dictionary<ushort, ItemBase>.ValueCollection.Enumerator enumerator = hub.inventory.UserInventory.Items.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current is Firearm)
					{
						num++;
					}
				}
			}
			return num < (int)InventoryLimits.GetCategoryLimit(ItemCategory.Firearm, hub);
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
		}
	}
}
