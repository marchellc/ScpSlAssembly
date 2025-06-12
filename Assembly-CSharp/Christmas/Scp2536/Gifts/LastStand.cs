using System.Linq;
using InventorySystem;
using InventorySystem.Items;
using PlayerRoles;
using UnityEngine;

namespace Christmas.Scp2536.Gifts;

public class LastStand : Scp2536ItemGift
{
	private const int MaxTeammatesRequirement = 3;

	private static bool _hasBeenGranted;

	public override UrgencyLevel Urgency => UrgencyLevel.One;

	public override float SecondsUntilAvailable => 360f;

	protected override Scp2536Reward[] Rewards => new Scp2536Reward[3]
	{
		new Scp2536Reward(ItemType.GunLogicer, 100f),
		new Scp2536Reward(ItemType.SCP500, 100f),
		new Scp2536Reward(ItemType.SCP1853, 100f)
	};

	public override bool CanBeGranted(ReferenceHub hub)
	{
		if (LastStand._hasBeenGranted || !base.CanBeGranted(hub))
		{
			return false;
		}
		Faction localFaction = hub.GetFaction();
		return ReferenceHub.AllHubs.Count((ReferenceHub x) => x.GetFaction() == localFaction) <= 3;
	}

	public override void ServerGrant(ReferenceHub hub)
	{
		LastStand._hasBeenGranted = true;
		ItemType type = base.GenerateRandomReward();
		hub.inventory.ServerAddItem(type, ItemAddReason.Scp2536, 0).GrantAmmoReward();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			LastStand._hasBeenGranted = false;
		};
	}
}
