using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class ItemPickupHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			InventoryExtensions.OnItemAdded += ItemPickupHandler.OnItemAdded;
		}

		private static void OnItemAdded(ReferenceHub ply, ItemBase ib, ItemPickupBase pickup)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			KeycardItem keycardItem = ib as KeycardItem;
			if (keycardItem != null && keycardItem.ItemTypeId == ItemType.KeycardO5)
			{
				AchievementHandlerBase.ServerAchieve(ply.connectionToClient, AchievementName.ExecutiveAccess);
				return;
			}
			if (ply.GetRoleId() == RoleTypeId.ClassD && ib is Firearm)
			{
				AchievementHandlerBase.ServerAchieve(ply.connectionToClient, AchievementName.ThatCanBeUseful);
			}
		}

		private const ItemType ExecutiveAccessCardId = ItemType.KeycardO5;
	}
}
