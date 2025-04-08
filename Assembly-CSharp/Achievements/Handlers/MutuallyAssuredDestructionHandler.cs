using System;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class MutuallyAssuredDestructionHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += MutuallyAssuredDestructionHandler.OnAnyPlayerDied;
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
			if (!MutuallyAssuredDestructionHandler.ValidDamage(attackerDamageHandler))
			{
				return;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub == null || hub.IsAlive())
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(attackerDamageHandler.Attacker.Role, victim.GetRoleId()))
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.MutuallyAssuredDestruction);
		}

		private static bool ValidDamage(AttackerDamageHandler attackerHandler)
		{
			if (attackerHandler is Scp018DamageHandler)
			{
				return true;
			}
			ExplosionDamageHandler explosionDamageHandler = attackerHandler as ExplosionDamageHandler;
			if (explosionDamageHandler == null)
			{
				return false;
			}
			ExplosionType explosionType = explosionDamageHandler.ExplosionType;
			return explosionType == ExplosionType.SCP018 || explosionType == ExplosionType.Grenade;
		}
	}
}
