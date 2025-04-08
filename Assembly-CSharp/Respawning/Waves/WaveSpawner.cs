using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.Spectating;
using Respawning.NamingRules;
using UnityEngine;
using UnityEngine.Pool;
using Utils.NonAllocLINQ;

namespace Respawning.Waves
{
	public static class WaveSpawner
	{
		public static bool AnyPlayersAvailable
		{
			get
			{
				return ReferenceHub.AllHubs.Any(new Func<ReferenceHub, bool>(WaveSpawner.CanBeSpawned));
			}
		}

		public static List<ReferenceHub> GetAvailablePlayers(Team spawningTeam)
		{
			return (from hub in ReferenceHub.AllHubs.Where(new Func<ReferenceHub, bool>(WaveSpawner.CanBeSpawned))
				orderby WaveSpawner.CalculatePriority(hub, spawningTeam) descending
				select hub).ToList<ReferenceHub>();
		}

		public static List<ReferenceHub> SpawnWave(SpawnableWaveBase wave)
		{
			List<ReferenceHub> list = NorthwoodLib.Pools.ListPool<ReferenceHub>.Shared.Rent();
			Team spawnableTeam = wave.TargetFaction.GetSpawnableTeam();
			List<ReferenceHub> availablePlayers = WaveSpawner.GetAvailablePlayers(spawnableTeam);
			int maxWaveSize = wave.MaxWaveSize;
			int num = Mathf.Min(availablePlayers.Count, maxWaveSize);
			if (num <= 0)
			{
				return list;
			}
			UnitNamingRule unitNamingRule;
			if (NamingRulesManager.TryGetNamingRule(spawnableTeam, out unitNamingRule))
			{
				NamingRulesManager.ServerGenerateName(spawnableTeam, unitNamingRule);
			}
			wave.PopulateQueue(WaveSpawner.SpawnQueue, num);
			RoleChangeReason roleChangeReason = ((wave is IMiniWave) ? RoleChangeReason.RespawnMiniwave : RoleChangeReason.Respawn);
			Dictionary<ReferenceHub, RoleTypeId> dictionary = CollectionPool<Dictionary<ReferenceHub, RoleTypeId>, KeyValuePair<ReferenceHub, RoleTypeId>>.Get();
			foreach (ReferenceHub referenceHub in availablePlayers)
			{
				if (list.Count >= maxWaveSize)
				{
					break;
				}
				try
				{
					RoleTypeId roleTypeId = WaveSpawner.SpawnQueue.Dequeue();
					dictionary.Add(referenceHub, roleTypeId);
				}
				catch (Exception ex)
				{
					if (referenceHub != null)
					{
						ServerLogs.AddLog(ServerLogs.Modules.ClassChange, "Player " + referenceHub.LoggedNameFromRefHub() + " couldn't be added to spawn wave. Err msg: " + ex.Message, ServerLogs.ServerLogType.GameEvent, false);
					}
					else
					{
						ServerLogs.AddLog(ServerLogs.Modules.ClassChange, "Couldn't add a player - target's ReferenceHub is null.", ServerLogs.ServerLogType.GameEvent, false);
					}
				}
			}
			WaveRespawningEventArgs waveRespawningEventArgs = new WaveRespawningEventArgs(wave, dictionary);
			ServerEvents.OnWaveRespawning(waveRespawningEventArgs);
			if (!waveRespawningEventArgs.IsAllowed)
			{
				list.Clear();
				CollectionPool<Dictionary<ReferenceHub, RoleTypeId>, KeyValuePair<ReferenceHub, RoleTypeId>>.Release(dictionary);
				WaveSpawner.SpawnQueue.Clear();
				return list;
			}
			foreach (KeyValuePair<Player, RoleTypeId> keyValuePair in waveRespawningEventArgs.Roles)
			{
				dictionary[keyValuePair.Key.ReferenceHub] = keyValuePair.Value;
			}
			CollectionPool<Dictionary<Player, RoleTypeId>, KeyValuePair<Player, RoleTypeId>>.Release(waveRespawningEventArgs.Roles);
			IAnnouncedWave announcedWave = wave as IAnnouncedWave;
			if (announcedWave != null)
			{
				announcedWave.Announcement.PlayAnnouncement();
			}
			foreach (KeyValuePair<ReferenceHub, RoleTypeId> keyValuePair2 in dictionary)
			{
				if (list.Count >= maxWaveSize)
				{
					break;
				}
				try
				{
					keyValuePair2.Key.roleManager.ServerSetRole(keyValuePair2.Value, roleChangeReason, RoleSpawnFlags.All);
					list.Add(keyValuePair2.Key);
					ServerLogs.AddLog(ServerLogs.Modules.ClassChange, string.Concat(new string[]
					{
						"Player ",
						keyValuePair2.Key.LoggedNameFromRefHub(),
						" respawned as ",
						keyValuePair2.Value.ToString(),
						"."
					}), ServerLogs.ServerLogType.GameEvent, false);
				}
				catch (Exception ex2)
				{
					if (keyValuePair2.Key != null)
					{
						ServerLogs.AddLog(ServerLogs.Modules.ClassChange, "Player " + keyValuePair2.Key.LoggedNameFromRefHub() + " couldn't be spawned. Err msg: " + ex2.Message, ServerLogs.ServerLogType.GameEvent, false);
					}
					else
					{
						ServerLogs.AddLog(ServerLogs.Modules.ClassChange, "Couldn't spawn a player - target's ReferenceHub is null.", ServerLogs.ServerLogType.GameEvent, false);
					}
				}
			}
			ServerEvents.OnWaveRespawned(new WaveRespawnedEventArgs(wave, dictionary.Keys.Select(new Func<ReferenceHub, Player>(Player.Get)).ToList<Player>()));
			if (list.Count > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.ClassChange, string.Format("{0} has successfully spawned {1} players as {2}!", "WaveSpawner", list.Count, spawnableTeam), ServerLogs.ServerLogType.GameEvent, false);
			}
			WaveSpawner.SpawnQueue.Clear();
			CollectionPool<Dictionary<ReferenceHub, RoleTypeId>, KeyValuePair<ReferenceHub, RoleTypeId>>.Release(dictionary);
			return list;
		}

		public static float CalculatePriority(ReferenceHub hub, Team targetTeam)
		{
			float num = global::UnityEngine.Random.Range(0f, 0.25f);
			SpectatorRole spectatorRole = hub.roleManager.CurrentRole as SpectatorRole;
			if (spectatorRole == null)
			{
				return -num;
			}
			num += spectatorRole.ActiveTime / 15f;
			Team team;
			if (!WaveSpawner.PreviousTeam.TryGetValue(hub, out team))
			{
				return num;
			}
			if (team == targetTeam)
			{
				num += 3f;
			}
			return num;
		}

		public static Team GetSpawnableTeam(this Faction faction)
		{
			Team team;
			if (faction != Faction.FoundationStaff)
			{
				if (faction == Faction.FoundationEnemy)
				{
					team = Team.ChaosInsurgency;
				}
				else
				{
					team = Team.OtherAlive;
				}
			}
			else
			{
				team = Team.FoundationForces;
			}
			return team;
		}

		public static bool CanBeSpawned(ReferenceHub player)
		{
			SpectatorRole spectatorRole = player.roleManager.CurrentRole as SpectatorRole;
			return spectatorRole != null && spectatorRole.ReadyToRespawn;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnServerRoleSet += WaveSpawner.OnServerRoleSet;
			CustomNetworkManager.OnClientReady += WaveSpawner.PreviousTeam.Clear;
		}

		private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			Team spawnableTeam = userHub.GetFaction().GetSpawnableTeam();
			WaveSpawner.PreviousTeam[userHub] = spawnableTeam;
		}

		public const float TimePerPoint = 15f;

		private const float MaxRandomPoints = 0.25f;

		private const float MinPoints = 0f;

		private const float SameFactionPoints = 3f;

		private static readonly Dictionary<ReferenceHub, Team> PreviousTeam = new Dictionary<ReferenceHub, Team>();

		private static readonly Queue<RoleTypeId> SpawnQueue = new Queue<RoleTypeId>();
	}
}
