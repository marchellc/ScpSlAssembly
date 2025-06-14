using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers;

public class TrilateralTerminationHandler : AchievementHandlerBase
{
	private const float TimeLimit = 3f;

	private const int KillsNeeded = 3;

	private static readonly Dictionary<ReferenceHub, Stopwatch> Timers = new Dictionary<ReferenceHub, Stopwatch>();

	private static readonly Dictionary<ReferenceHub, int> Kills = new Dictionary<ReferenceHub, int>();

	internal override void OnInitialize()
	{
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
	}

	internal override void OnRoundStarted()
	{
		TrilateralTerminationHandler.Kills.Clear();
		TrilateralTerminationHandler.Timers.Clear();
	}

	private static void OnAnyPlayerDied(ReferenceHub victim, DamageHandlerBase handler)
	{
		if (!NetworkServer.active || !(handler is AttackerDamageHandler attackerDamageHandler) || (!(handler is DisruptorDamageHandler) && !(handler is ExplosionDamageHandler { ExplosionType: ExplosionType.Disruptor })))
		{
			return;
		}
		ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
		if (!(hub == null) && HitboxIdentity.IsEnemy(attackerDamageHandler.Attacker.Role, victim.GetRoleId()))
		{
			if (!TrilateralTerminationHandler.Kills.TryGetValue(hub, out var value))
			{
				value = 0;
			}
			value = (TrilateralTerminationHandler.Kills[hub] = value + 1);
			if (!TrilateralTerminationHandler.Timers.TryGetValue(hub, out var value2))
			{
				TrilateralTerminationHandler.Timers.Add(hub, Stopwatch.StartNew());
			}
			else if (value2.Elapsed.TotalSeconds > 3.0)
			{
				TrilateralTerminationHandler.Timers[hub].Restart();
				TrilateralTerminationHandler.Kills[hub] = 1;
			}
			else if (value >= 3)
			{
				TrilateralTerminationHandler.Timers.Remove(hub);
				TrilateralTerminationHandler.Kills.Remove(hub);
				AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.TrilateralTermination);
			}
		}
	}
}
