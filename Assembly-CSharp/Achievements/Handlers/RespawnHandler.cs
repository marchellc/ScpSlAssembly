using System;
using Mirror;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class RespawnHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerRoleManager.OnServerRoleSet += RespawnHandler.OnRespawned;
		}

		private static void OnRespawned(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			PlayerRoleBase playerRoleBase;
			if (reason == RoleChangeReason.Respawn || reason == RoleChangeReason.RespawnMiniwave || !PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(newRole, out playerRoleBase))
			{
				return;
			}
			NetworkConnection connectionToClient = userHub.connectionToClient;
			Team team = playerRoleBase.Team;
			if (team == Team.FoundationForces)
			{
				AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.LightsOut);
				return;
			}
			if (team == Team.ChaosInsurgency)
			{
				AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.DeltaCommand);
			}
		}
	}
}
