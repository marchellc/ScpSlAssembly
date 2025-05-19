using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

namespace Christmas.Scp2536.Gifts;

public class NonStandardWeapons : Scp2536ItemGift
{
	private const int OverrideWeaponGiftChance = 10;

	public override UrgencyLevel Urgency => UrgencyLevel.Exclusive;

	internal override bool IgnoredByRandomness => true;

	protected override Scp2536Reward[] Rewards => new Scp2536Reward[3]
	{
		new Scp2536Reward(ItemType.ParticleDisruptor, 35f),
		new Scp2536Reward(ItemType.Jailbird, 35f),
		new Scp2536Reward(ItemType.GunCom45, 30f)
	};

	public override void ServerGrant(ReferenceHub hub)
	{
		ItemType type = GenerateRandomReward();
		hub.inventory.ServerAddItem(type, ItemAddReason.Scp2536, 0).GrantAmmoReward();
	}

	public static bool CanOverrideGift(Scp2536GiftBase gift)
	{
		if (Random.Range(1, 101) > 10)
		{
			return false;
		}
		if (!(gift is TierOneWeapons))
		{
			if (!(gift is TierTwoWeapons))
			{
				if (!(gift is TierThreeWeapons))
				{
					if (gift is TierFourWeapons)
					{
						return true;
					}
					return false;
				}
				return true;
			}
			return true;
		}
		return true;
	}
}
