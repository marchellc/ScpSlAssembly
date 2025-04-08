using System;
using Mirror;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class TurnThemAllHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerRoleManager.OnServerRoleSet += TurnThemAllHandler.OnRoleChanged;
		}

		internal override void OnRoundStarted()
		{
			TurnThemAllHandler._healedPlayers = 0;
			TurnThemAllHandler._alreadyAchieved = false;
		}

		private static void OnRoleChanged(ReferenceHub userHub, RoleTypeId newClass, RoleChangeReason reason)
		{
			if (!NetworkServer.active || TurnThemAllHandler._alreadyAchieved || newClass != RoleTypeId.Scp0492 || reason != RoleChangeReason.Revived)
			{
				return;
			}
			TurnThemAllHandler._healedPlayers++;
			if (TurnThemAllHandler._healedPlayers < 10)
			{
				return;
			}
			NetworkConnection networkConnection = null;
			bool flag = false;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.GetRoleId() == RoleTypeId.Scp049)
				{
					if (flag)
					{
						return;
					}
					networkConnection = referenceHub.networkIdentity.connectionToClient;
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(networkConnection, AchievementName.TurnThemAll);
		}

		private const int TargetCured = 10;

		private static int _healedPlayers;

		private static bool _alreadyAchieved;
	}
}
