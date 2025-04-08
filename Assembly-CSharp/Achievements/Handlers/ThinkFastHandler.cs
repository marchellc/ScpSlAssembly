using System;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class ThinkFastHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += ThinkFastHandler.OnAnyPlayerDied;
		}

		private static void OnAnyPlayerDied(ReferenceHub victim, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			if (!(handler is Scp018DamageHandler))
			{
				ExplosionDamageHandler explosionDamageHandler = handler as ExplosionDamageHandler;
				if (explosionDamageHandler == null || explosionDamageHandler.ExplosionType != ExplosionType.SCP018)
				{
					return;
				}
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub == null)
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(attackerDamageHandler.Attacker.Role, victim.GetRoleId()))
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.ThinkFast);
		}
	}
}
