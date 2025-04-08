using System;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

namespace Christmas.Scp2536.Gifts
{
	public class NonStandardWeapons : Scp2536ItemGift
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
					new Scp2536Reward(ItemType.ParticleDisruptor, 35f),
					new Scp2536Reward(ItemType.Jailbird, 35f),
					new Scp2536Reward(ItemType.GunCom45, 30f)
				};
			}
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			ItemType itemType = base.GenerateRandomReward();
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
		}

		public static bool CanOverrideGift(Scp2536GiftBase gift)
		{
			return global::UnityEngine.Random.Range(1, 101) <= 10 && (gift is TierOneWeapons || gift is TierTwoWeapons || gift is TierThreeWeapons || gift is TierFourWeapons);
		}

		private const int OverrideWeaponGiftChance = 10;
	}
}
