using InventorySystem;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;

namespace Christmas.Scp2536.Gifts;

public class TierOneWeapons : Scp2536ItemGift
{
	public override UrgencyLevel Urgency => UrgencyLevel.Three;

	public override float SecondsUntilAvailable => 120f;

	public override float SecondsUntilUnavailable => 240f;

	protected override Scp2536Reward[] Rewards => new Scp2536Reward[2]
	{
		new Scp2536Reward(ItemType.GunCOM15, 50f),
		new Scp2536Reward(ItemType.GunCOM18, 50f)
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
		ItemType type = base.GenerateRandomReward();
		hub.inventory.ServerAddItem(type, ItemAddReason.Scp2536, 0).GrantAmmoReward();
	}
}
