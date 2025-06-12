using Mirror;
using PlayerRoles;

namespace Achievements.Handlers;

public class TurnThemAllHandler : AchievementHandlerBase
{
	private const int TargetCured = 10;

	private static int _healedPlayers;

	private static bool _alreadyAchieved;

	internal override void OnInitialize()
	{
		PlayerRoleManager.OnServerRoleSet += OnRoleChanged;
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
		NetworkConnection conn = null;
		bool flag = false;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.GetRoleId() == RoleTypeId.Scp049)
			{
				if (flag)
				{
					return;
				}
				conn = allHub.networkIdentity.connectionToClient;
				flag = true;
			}
		}
		if (flag)
		{
			AchievementHandlerBase.ServerAchieve(conn, AchievementName.TurnThemAll);
		}
	}
}
