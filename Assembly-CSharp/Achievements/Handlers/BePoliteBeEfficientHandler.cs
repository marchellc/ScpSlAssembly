using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers;

public class BePoliteBeEfficientHandler : AchievementHandlerBase
{
	private const float TimeLimit = 30f;

	private const int KillsTarget = 5;

	private static readonly Dictionary<ReferenceHub, Stopwatch> Timers = new Dictionary<ReferenceHub, Stopwatch>();

	private static readonly Dictionary<ReferenceHub, int> Kills = new Dictionary<ReferenceHub, int>();

	private static readonly HashSet<uint> AlreadyAchieved = new HashSet<uint>();

	internal override void OnInitialize()
	{
		PlayerStats.OnAnyPlayerDied += HandleDeath;
	}

	internal override void OnRoundStarted()
	{
		BePoliteBeEfficientHandler.Kills.Clear();
		BePoliteBeEfficientHandler.Timers.Clear();
		BePoliteBeEfficientHandler.AlreadyAchieved.Clear();
	}

	private static void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
	{
		if (NetworkServer.active && handler is FirearmDamageHandler { Attacker: { Hub: var hub } attacker } && HitboxIdentity.IsEnemy(attacker.Role, deadPlayer.GetRoleId()) && !BePoliteBeEfficientHandler.AlreadyAchieved.Contains(attacker.NetId) && !(hub == null))
		{
			if (!BePoliteBeEfficientHandler.Kills.TryGetValue(hub, out var value))
			{
				value = 0;
			}
			value = (BePoliteBeEfficientHandler.Kills[hub] = value + 1);
			if (!BePoliteBeEfficientHandler.Timers.TryGetValue(hub, out var value2))
			{
				BePoliteBeEfficientHandler.Timers.Add(hub, Stopwatch.StartNew());
			}
			else if (value2.Elapsed.TotalSeconds > 30.0)
			{
				BePoliteBeEfficientHandler.Timers[hub].Restart();
				BePoliteBeEfficientHandler.Kills[hub] = 1;
			}
			else if (value >= 5)
			{
				BePoliteBeEfficientHandler.AlreadyAchieved.Add(attacker.NetId);
				BePoliteBeEfficientHandler.Kills.Remove(attacker.Hub);
				BePoliteBeEfficientHandler.Timers.Remove(attacker.Hub);
				AchievementHandlerBase.ServerAchieve(attacker.Hub.networkIdentity.connectionToClient, AchievementName.BePoliteBeEfficient);
			}
		}
	}
}
