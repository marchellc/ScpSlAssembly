using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables;
using PlayerRoles;
using Scp914;

namespace Respawning.Objectives;

public class ScpItemPickupObjective : HumanObjectiveBase<PickupObjectiveFootprint>
{
	private const float PickupItemTimer = -5f;

	private const float PickupItemInfluence = 2f;

	private const float PickupConsumableTimer = -2f;

	private const float PickupConsumableInfluence = 1f;

	private static readonly HashSet<ushort> BlacklistedItems = new HashSet<ushort>();

	protected override PickupObjectiveFootprint ClientCreateFootprint()
	{
		return new PickupObjectiveFootprint();
	}

	protected override void OnInstanceCreated()
	{
		base.OnInstanceCreated();
		InventoryExtensions.OnItemAdded += OnItemAdded;
		Scp914Upgrader.OnUpgraded += OnPickupUpgraded;
	}

	protected override void OnInstanceReset()
	{
		base.OnInstanceReset();
		BlacklistedItems.Clear();
	}

	private void OnPickupUpgraded(Scp914Result result, Scp914KnobSetting _)
	{
		if (result.ResultingPickups != null)
		{
			ItemPickupBase[] resultingPickups = result.ResultingPickups;
			foreach (ItemPickupBase itemPickupBase in resultingPickups)
			{
				if (!(itemPickupBase == null))
				{
					BlacklistedItems.Add(itemPickupBase.Info.Serial);
				}
			}
		}
		if (result.ResultingItems == null)
		{
			return;
		}
		ItemBase[] resultingItems = result.ResultingItems;
		foreach (ItemBase itemBase in resultingItems)
		{
			if (!(itemBase == null))
			{
				BlacklistedItems.Add(itemBase.ItemSerial);
			}
		}
	}

	private void OnItemAdded(ReferenceHub hub, ItemBase item, ItemPickupBase pickup)
	{
		if (!(pickup == null) && item.Category == ItemCategory.SCPItem && item.ItemTypeId != ItemType.SCP330 && BlacklistedItems.Add(pickup.Info.Serial))
		{
			Faction faction = hub.GetFaction();
			float num;
			float num2;
			if (item is Consumable)
			{
				num = 1f;
				num2 = -2f;
			}
			else
			{
				num = 2f;
				num2 = -5f;
			}
			GrantInfluence(faction, num);
			ReduceTimer(faction, num2);
			base.ObjectiveFootprint = new PickupObjectiveFootprint
			{
				InfluenceReward = num,
				TimeReward = num2,
				AchievingPlayer = new ObjectiveHubFootprint(hub),
				PickupType = item.ItemTypeId
			};
			ServerSendUpdate();
		}
	}
}
