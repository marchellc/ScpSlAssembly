using System;
using System.Collections.Generic;
using System.Diagnostics;
using Footprinting;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class BePoliteBeEfficientHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += BePoliteBeEfficientHandler.HandleDeath;
		}

		internal override void OnRoundStarted()
		{
			BePoliteBeEfficientHandler.Kills.Clear();
			BePoliteBeEfficientHandler.Timers.Clear();
			BePoliteBeEfficientHandler.AlreadyAchieved.Clear();
		}

		private static void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
		{
			if (NetworkServer.active)
			{
				FirearmDamageHandler firearmDamageHandler = handler as FirearmDamageHandler;
				if (firearmDamageHandler != null)
				{
					Footprint attacker = firearmDamageHandler.Attacker;
					ReferenceHub hub = attacker.Hub;
					if (!HitboxIdentity.IsEnemy(attacker.Role, deadPlayer.GetRoleId()))
					{
						return;
					}
					if (BePoliteBeEfficientHandler.AlreadyAchieved.Contains(attacker.NetId) || hub == null)
					{
						return;
					}
					int num;
					if (!BePoliteBeEfficientHandler.Kills.TryGetValue(hub, out num))
					{
						num = 0;
					}
					num = (BePoliteBeEfficientHandler.Kills[hub] = num + 1);
					Stopwatch stopwatch;
					if (!BePoliteBeEfficientHandler.Timers.TryGetValue(hub, out stopwatch))
					{
						BePoliteBeEfficientHandler.Timers.Add(hub, Stopwatch.StartNew());
						return;
					}
					if (stopwatch.Elapsed.TotalSeconds > 30.0)
					{
						BePoliteBeEfficientHandler.Timers[hub].Restart();
						BePoliteBeEfficientHandler.Kills[hub] = 1;
						return;
					}
					if (num < 5)
					{
						return;
					}
					BePoliteBeEfficientHandler.AlreadyAchieved.Add(attacker.NetId);
					BePoliteBeEfficientHandler.Kills.Remove(attacker.Hub);
					BePoliteBeEfficientHandler.Timers.Remove(attacker.Hub);
					AchievementHandlerBase.ServerAchieve(attacker.Hub.networkIdentity.connectionToClient, AchievementName.BePoliteBeEfficient);
					return;
				}
			}
		}

		private const float TimeLimit = 30f;

		private const int KillsTarget = 5;

		private static readonly Dictionary<ReferenceHub, Stopwatch> Timers = new Dictionary<ReferenceHub, Stopwatch>();

		private static readonly Dictionary<ReferenceHub, int> Kills = new Dictionary<ReferenceHub, int>();

		private static readonly HashSet<uint> AlreadyAchieved = new HashSet<uint>();
	}
}
