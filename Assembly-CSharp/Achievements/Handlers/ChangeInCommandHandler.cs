using System;
using InventorySystem.Disarming;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class ChangeInCommandHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			DisarmingHandlers.OnPlayerDisarmed += this.OnPlayerDisarmed;
		}

		private void OnPlayerDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			if (disarmerHub == null || targetHub == null)
			{
				return;
			}
			if (targetHub.GetTeam() == Team.FoundationForces && disarmerHub.GetRoleId() == RoleTypeId.ClassD)
			{
				AchievementHandlerBase.ServerAchieve(disarmerHub.networkIdentity.connectionToClient, AchievementName.ChangeInCommand);
			}
		}
	}
}
