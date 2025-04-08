using System;
using System.Collections.Generic;
using InventorySystem.Items.Usables;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class RuleBreakerHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			UsableItemsController.ServerOnUsingCompleted += RuleBreakerHandler.OnUsedScp330;
			PlayerRoleManager.OnServerRoleSet += RuleBreakerHandler.OnServerRoleSet;
		}

		internal override void OnRoundStarted()
		{
			RuleBreakerHandler.CandiesEaten.Clear();
		}

		private static void OnServerRoleSet(ReferenceHub hub, RoleTypeId roleTypeId, RoleChangeReason changeReason)
		{
			RuleBreakerHandler.CandiesEaten.Remove(hub);
		}

		private static void OnUsedScp330(ReferenceHub hub, UsableItem item)
		{
			if (item.ItemTypeId != ItemType.SCP330)
			{
				return;
			}
			int valueOrDefault = RuleBreakerHandler.CandiesEaten.GetValueOrDefault(hub, 0);
			if ((RuleBreakerHandler.CandiesEaten[hub] = valueOrDefault + 1) < 3)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.RuleBreaker);
		}

		private const int CandiesNeeded = 3;

		private static readonly Dictionary<ReferenceHub, int> CandiesEaten = new Dictionary<ReferenceHub, int>();
	}
}
