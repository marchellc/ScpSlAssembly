using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentralAuth;
using GameCore;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.RoleAssign
{
	public static class RoleAssigner
	{
		public static event Action OnPlayersSpawned;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				RoleAssigner._spawned = false;
				RoleAssigner.AlreadySpawnedPlayers.Clear();
			};
			PlayerAuthenticationManager.OnInstanceModeChanged += RoleAssigner.CheckLateJoin;
			CharacterClassManager.OnRoundStarted += RoleAssigner.OnRoundStarted;
		}

		private static void OnRoundStarted()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			string @string = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");
			int length = @string.Length;
			if (RoleAssigner._prevQueueSize < length)
			{
				RoleAssigner._totalQueue = new Team[length];
				RoleAssigner._humanQueue = new Team[length];
				RoleAssigner._prevQueueSize = length;
			}
			int num = 0;
			int num2 = 0;
			string text = @string;
			for (int i = 0; i < text.Length; i++)
			{
				Team team = (Team)(text[i] - '0');
				if (Enum.IsDefined(typeof(Team), team))
				{
					if (team != Team.SCPs)
					{
						RoleAssigner._humanQueue[num++] = team;
					}
					RoleAssigner._totalQueue[num2++] = team;
				}
			}
			if (num2 == 0)
			{
				throw new InvalidOperationException("Failed to assign roles, queue has failed to load.");
			}
			RoleAssigner._spawned = true;
			RoleAssigner.LateJoinTimer.Restart();
			int num3 = ReferenceHub.AllHubs.Count((ReferenceHub x) => RoleAssigner.CheckPlayer(x));
			int num4 = 0;
			for (int j = 0; j < num3; j++)
			{
				if (RoleAssigner._totalQueue[j % num2] == Team.SCPs)
				{
					num4++;
				}
			}
			ScpSpawner.SpawnScps(num4);
			HumanSpawner.SpawnHumans(RoleAssigner._humanQueue, num);
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.IsAlive())
				{
					RoleAssigner.AlreadySpawnedPlayers.Add(referenceHub.authManager.UserId);
				}
			}
			Action onPlayersSpawned = RoleAssigner.OnPlayersSpawned;
			if (onPlayersSpawned == null)
			{
				return;
			}
			onPlayersSpawned();
		}

		private static void CheckLateJoin(ReferenceHub hub, ClientInstanceMode cim)
		{
			if (!NetworkServer.active || !RoleAssigner.CheckPlayer(hub) || !RoleAssigner._spawned)
			{
				return;
			}
			float @float = ConfigFile.ServerConfig.GetFloat("late_join_time", 0f);
			if (!RoleAssigner.AlreadySpawnedPlayers.Add(hub.authManager.UserId) || RoleAssigner.LateJoinTimer.Elapsed.TotalSeconds > (double)@float)
			{
				hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.LateJoin, RoleSpawnFlags.All);
				return;
			}
			HumanSpawner.SpawnLate(hub);
		}

		public static bool CheckPlayer(ReferenceHub hub)
		{
			if (!hub.IsAlive())
			{
				SpectatorRole spectatorRole = hub.roleManager.CurrentRole as SpectatorRole;
				if (spectatorRole == null || spectatorRole.ReadyToRespawn)
				{
					ClientInstanceMode mode = hub.Mode;
					return mode - ClientInstanceMode.ReadyClient <= 1 || mode == ClientInstanceMode.Dummy;
				}
			}
			return false;
		}

		private static readonly Stopwatch LateJoinTimer = new Stopwatch();

		private static readonly HashSet<string> AlreadySpawnedPlayers = new HashSet<string>();

		private const string DefaultQueue = "4014314031441404134041434414";

		private const string SpawnQueueKey = "team_respawn_queue";

		private const string LateJoinKey = "late_join_time";

		private static bool _spawned;

		private static int _prevQueueSize;

		private static Team[] _totalQueue;

		private static Team[] _humanQueue;
	}
}
