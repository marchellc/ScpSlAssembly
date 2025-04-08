using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class TrilateralTerminationHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += TrilateralTerminationHandler.OnAnyPlayerDied;
		}

		internal override void OnRoundStarted()
		{
			TrilateralTerminationHandler.Kills.Clear();
			TrilateralTerminationHandler.Timers.Clear();
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
			if (!(handler is DisruptorDamageHandler))
			{
				ExplosionDamageHandler explosionDamageHandler = handler as ExplosionDamageHandler;
				if (explosionDamageHandler == null || explosionDamageHandler.ExplosionType != ExplosionType.Disruptor)
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
			int num;
			if (!TrilateralTerminationHandler.Kills.TryGetValue(hub, out num))
			{
				num = 0;
			}
			num = (TrilateralTerminationHandler.Kills[hub] = num + 1);
			Stopwatch stopwatch;
			if (!TrilateralTerminationHandler.Timers.TryGetValue(hub, out stopwatch))
			{
				TrilateralTerminationHandler.Timers.Add(hub, Stopwatch.StartNew());
				return;
			}
			if (stopwatch.Elapsed.TotalSeconds > 3.0)
			{
				TrilateralTerminationHandler.Timers[hub].Restart();
				TrilateralTerminationHandler.Kills[hub] = 1;
				return;
			}
			if (num < 3)
			{
				return;
			}
			TrilateralTerminationHandler.Timers.Remove(hub);
			TrilateralTerminationHandler.Kills.Remove(hub);
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.TrilateralTermination);
		}

		private const float TimeLimit = 3f;

		private const int KillsNeeded = 3;

		private static readonly Dictionary<ReferenceHub, Stopwatch> Timers = new Dictionary<ReferenceHub, Stopwatch>();

		private static readonly Dictionary<ReferenceHub, int> Kills = new Dictionary<ReferenceHub, int>();
	}
}
