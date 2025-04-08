using System;
using System.Collections.Generic;
using System.Diagnostics;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class HatsOffToYouHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += this.HandleDeath;
			StatusEffectBase.OnDisabled += this.HandleEffectDisabled;
		}

		internal override void OnRoundStarted()
		{
			HatsOffToYouHandler.Timers.Clear();
		}

		private void HandleEffectDisabled(StatusEffectBase effectBase)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Invisible invisible = effectBase as Invisible;
			if (invisible == null)
			{
				return;
			}
			Stopwatch stopwatch;
			if (HatsOffToYouHandler.Timers.TryGetValue(invisible.Hub, out stopwatch))
			{
				if (stopwatch.Elapsed.TotalSeconds >= 5.0)
				{
					stopwatch.Restart();
					return;
				}
			}
			else
			{
				HatsOffToYouHandler.Timers[invisible.Hub] = Stopwatch.StartNew();
			}
		}

		private void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null || attackerDamageHandler.Attacker.Hub == null)
			{
				return;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub.IsSCP(true) || !HitboxIdentity.IsEnemy(hub, deadPlayer))
			{
				return;
			}
			Stopwatch stopwatch;
			if (!HatsOffToYouHandler.Timers.TryGetValue(hub, out stopwatch))
			{
				return;
			}
			if (stopwatch.Elapsed.TotalSeconds <= 5.0)
			{
				AchievementHandlerBase.ServerAchieve(hub.networkIdentity.connectionToClient, AchievementName.HatsOffToYou);
			}
			HatsOffToYouHandler.Timers.Remove(hub);
		}

		private const float KillWindowSeconds = 5f;

		private static readonly Dictionary<ReferenceHub, Stopwatch> Timers = new Dictionary<ReferenceHub, Stopwatch>();
	}
}
