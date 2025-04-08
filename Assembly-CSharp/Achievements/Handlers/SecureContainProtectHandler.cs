using System;
using PlayerRoles;
using PlayerStatsSystem;
using Utils.NonAllocLINQ;

namespace Achievements.Handlers
{
	public class SecureContainProtectHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += this.PlayerDied;
		}

		private void PlayerDied(ReferenceHub plr, DamageHandlerBase handler)
		{
			if (plr.GetTeam() == Team.SCPs)
			{
				AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
				if (attackerDamageHandler != null && !(attackerDamageHandler.Attacker.Hub == null) && attackerDamageHandler.Attacker.Hub.GetTeam() == Team.FoundationForces)
				{
					if (!ReferenceHub.AllHubs.Any((ReferenceHub hub) => hub.GetTeam() == Team.SCPs && hub.GetRoleId() != RoleTypeId.Scp079 && hub.GetRoleId() != RoleTypeId.Scp0492 && hub != plr))
					{
						AchievementHandlerBase.ServerAchieve(attackerDamageHandler.Attacker.Hub.connectionToClient, AchievementName.SecureContainProtect);
					}
					return;
				}
			}
		}
	}
}
