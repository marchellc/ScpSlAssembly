using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables;
using PlayerRoles;
using Scp914;

namespace Respawning.Objectives
{
	public class ScpItemPickupObjective : HumanObjectiveBase<PickupObjectiveFootprint>
	{
		protected override PickupObjectiveFootprint ClientCreateFootprint()
		{
			return new PickupObjectiveFootprint();
		}

		protected override void OnInstanceCreated()
		{
			base.OnInstanceCreated();
			InventoryExtensions.OnItemAdded += this.OnItemAdded;
			Scp914Upgrader.OnUpgraded += this.OnPickupUpgraded;
		}

		protected override void OnInstanceReset()
		{
			base.OnInstanceReset();
			ScpItemPickupObjective.BlacklistedItems.Clear();
		}

		private void OnPickupUpgraded(Scp914Result result, Scp914KnobSetting _)
		{
			if (result.ResultingPickups != null)
			{
				foreach (ItemPickupBase itemPickupBase in result.ResultingPickups)
				{
					if (!(itemPickupBase == null))
					{
						ScpItemPickupObjective.BlacklistedItems.Add(itemPickupBase.Info.Serial);
					}
				}
			}
			if (result.ResultingItems != null)
			{
				foreach (ItemBase itemBase in result.ResultingItems)
				{
					if (!(itemBase == null))
					{
						ScpItemPickupObjective.BlacklistedItems.Add(itemBase.ItemSerial);
					}
				}
			}
		}

		private void OnItemAdded(ReferenceHub hub, ItemBase item, ItemPickupBase pickup)
		{
			if (pickup == null)
			{
				return;
			}
			if (item.Category != ItemCategory.SCPItem)
			{
				return;
			}
			if (item.ItemTypeId == ItemType.SCP330)
			{
				return;
			}
			if (!ScpItemPickupObjective.BlacklistedItems.Add(pickup.Info.Serial))
			{
				return;
			}
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
			base.GrantInfluence(faction, num);
			base.ReduceTimer(faction, num2);
			base.ObjectiveFootprint = new PickupObjectiveFootprint
			{
				InfluenceReward = num,
				TimeReward = num2,
				AchievingPlayer = new ObjectiveHubFootprint(hub, RoleTypeId.None),
				PickupType = item.ItemTypeId
			};
			base.ServerSendUpdate();
		}

		private const float PickupItemTimer = -5f;

		private const float PickupItemInfluence = 2f;

		private const float PickupConsumableTimer = -2f;

		private const float PickupConsumableInfluence = 1f;

		private static readonly HashSet<ushort> BlacklistedItems = new HashSet<ushort>();
	}
}
