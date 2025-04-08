using System;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Pickups;
using MapGeneration;
using PlayerRoles;
using Scp914;

namespace Achievements.Handlers
{
	public class Scp914UpgradeHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			Scp914Upgrader.OnInventoryItemUpgraded = (Action<ItemBase, Scp914KnobSetting>)Delegate.Combine(Scp914Upgrader.OnInventoryItemUpgraded, new Action<ItemBase, Scp914KnobSetting>(Scp914UpgradeHandler.ItemUpgraded));
			Scp914Upgrader.OnPickupUpgraded = (Action<ItemPickupBase, Scp914KnobSetting>)Delegate.Combine(Scp914Upgrader.OnPickupUpgraded, new Action<ItemPickupBase, Scp914KnobSetting>(Scp914UpgradeHandler.PickupUpgraded));
		}

		private static void ItemUpgraded(ItemBase item, Scp914KnobSetting sett)
		{
			if (item.Owner.GetRoleId() == RoleTypeId.Scientist && item is KeycardItem && Scp914UpgradeHandler.AnyDClassIn914)
			{
				AchievementHandlerBase.ServerAchieve(item.OwnerInventory.connectionToClient, AchievementName.Friendship);
			}
		}

		private static void PickupUpgraded(ItemPickupBase ipb, Scp914KnobSetting sett)
		{
			if (ipb is KeycardPickup && ipb.PreviousOwner.Role == RoleTypeId.Scientist && Scp914UpgradeHandler.AnyDClassIn914 && ipb.PreviousOwner.Hub != null)
			{
				AchievementHandlerBase.ServerAchieve(ipb.PreviousOwner.Hub.networkIdentity.connectionToClient, AchievementName.Friendship);
			}
		}

		private static bool AnyDClassIn914
		{
			get
			{
				foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
				{
					if (referenceHub.GetRoleId() == RoleTypeId.ClassD)
					{
						RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(referenceHub.transform.position);
						if (roomIdentifier != null && roomIdentifier.Name == RoomName.Lcz914)
						{
							return true;
						}
					}
				}
				return false;
			}
		}
	}
}
