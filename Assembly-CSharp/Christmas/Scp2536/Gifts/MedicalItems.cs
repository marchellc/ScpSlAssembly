using System;
using InventorySystem;
using InventorySystem.Items;
using PlayerStatsSystem;

namespace Christmas.Scp2536.Gifts
{
	public class MedicalItems : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Two;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.Medkit, 50f),
					new Scp2536Reward(ItemType.Painkillers, 40f),
					new Scp2536Reward(ItemType.AntiSCP207, 10f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			HealthStat healthStat;
			return base.CanBeGranted(hub) && hub.playerStats.TryGetModule<HealthStat>(out healthStat) && healthStat.CurValue <= healthStat.MaxValue * 0.5f;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
		}
	}
}
