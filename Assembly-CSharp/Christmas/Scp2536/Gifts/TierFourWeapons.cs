using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;

namespace Christmas.Scp2536.Gifts;

public class TierFourWeapons : Scp2536ItemGift
{
	public override UrgencyLevel Urgency => UrgencyLevel.Three;

	public override float SecondsUntilAvailable => 600f;

	protected override Scp2536Reward[] Rewards => new Scp2536Reward[2]
	{
		new Scp2536Reward(ItemType.GunLogicer, 50f),
		new Scp2536Reward(ItemType.GunFRMG0, 50f)
	};

	public override bool CanBeGranted(ReferenceHub hub)
	{
		if (!base.CanBeGranted(hub))
		{
			return false;
		}
		int num = 0;
		foreach (ItemBase value in hub.inventory.UserInventory.Items.Values)
		{
			if (value is Firearm)
			{
				num++;
			}
		}
		return num < InventoryLimits.GetCategoryLimit(ItemCategory.Firearm, hub);
	}

	public override void ServerGrant(ReferenceHub hub)
	{
		ItemType type = GenerateRandomReward();
		hub.inventory.ServerAddItem(type, ItemAddReason.Scp2536, 0).GrantAmmoReward();
	}
}
