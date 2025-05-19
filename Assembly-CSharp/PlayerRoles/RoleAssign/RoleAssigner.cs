using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentralAuth;
using GameCore;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.RoleAssign;

public static class RoleAssigner
{
	public const float ScpOverflowMaxHealthMultiplier = 1.1f;

	private static readonly Stopwatch LateJoinTimer = new Stopwatch();

	private static readonly HashSet<string> AlreadySpawnedPlayers = new HashSet<string>();

	private const string DefaultQueue = "4014314031441404134041434414";

	private const string SpawnQueueKey = "team_respawn_queue";

	private const string LateJoinKey = "late_join_time";

	private const string AllowScpOverflowSettingKey = "allow_scp_overflow";

	private const float ScpOverflowMaxHumeShieldMultiplier = 0.05f;

	private static bool _spawned;

	private static int _prevQueueSize;

	private static Team[] _totalQueue;

	private static Team[] _humanQueue;

	public static bool ScpsOverflowing { get; set; }

	public static float ScpOverflowMaxHsMultiplier { get; private set; }

	public static event Action OnPlayersSpawned;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			_spawned = false;
			AlreadySpawnedPlayers.Clear();
		};
		PlayerAuthenticationManager.OnInstanceModeChanged += CheckLateJoin;
		CharacterClassManager.OnRoundStarted += OnRoundStarted;
	}

	private static void OnRoundStarted()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		string @string = ConfigFile.ServerConfig.GetString("team_respawn_queue", "4014314031441404134041434414");
		int length = @string.Length;
		if (_prevQueueSize < length)
		{
			_totalQueue = new Team[length];
			_humanQueue = new Team[length];
			_prevQueueSize = length;
		}
		int queueLength = 0;
		int num = 0;
		string text = @string;
		for (int i = 0; i < text.Length; i++)
		{
			Team team = (Team)(text[i] - 48);
			if (Enum.IsDefined(typeof(Team), team))
			{
				if (team != 0)
				{
					_humanQueue[queueLength++] = team;
				}
				_totalQueue[num++] = team;
			}
		}
		if (num == 0)
		{
			throw new InvalidOperationException("Failed to assign roles, queue has failed to load.");
		}
		_spawned = true;
		LateJoinTimer.Restart();
		ScpsOverflowing = false;
		bool @bool = ConfigFile.ServerConfig.GetBool("allow_scp_overflow");
		int maxSpawnableScps = ScpSpawner.MaxSpawnableScps;
		int num2 = ReferenceHub.AllHubs.Count((ReferenceHub x) => CheckPlayer(x));
		int num3 = 0;
		for (int j = 0; j < num2; j++)
		{
			if (_totalQueue[j % num] == Team.SCPs)
			{
				num3++;
				if (!(num3 != maxSpawnableScps || @bool))
				{
					ScpOverflowMaxHsMultiplier = 1f + (float)(num2 - j) * 0.05f;
					ScpsOverflowing = true;
					break;
				}
			}
		}
		ScpSpawner.SpawnScps(num3);
		HumanSpawner.SpawnHumans(_humanQueue, queueLength);
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.IsAlive())
			{
				AlreadySpawnedPlayers.Add(allHub.authManager.UserId);
			}
		}
		RoleAssigner.OnPlayersSpawned?.Invoke();
	}

	private static void CheckLateJoin(ReferenceHub hub, ClientInstanceMode cim)
	{
		if (NetworkServer.active && CheckPlayer(hub) && _spawned)
		{
			float @float = ConfigFile.ServerConfig.GetFloat("late_join_time");
			if (!AlreadySpawnedPlayers.Add(hub.authManager.UserId) || LateJoinTimer.Elapsed.TotalSeconds > (double)@float)
			{
				hub.roleManager.ServerSetRole(RoleTypeId.Spectator, RoleChangeReason.LateJoin);
			}
			else
			{
				HumanSpawner.SpawnLate(hub);
			}
		}
	}

	public static bool CheckPlayer(ReferenceHub hub)
	{
		if (hub.IsAlive() || hub.roleManager.CurrentRole is SpectatorRole { ReadyToRespawn: false })
		{
			return false;
		}
		ClientInstanceMode mode = hub.Mode;
		if (mode - 1 <= ClientInstanceMode.ReadyClient || mode == ClientInstanceMode.Dummy)
		{
			return true;
		}
		return false;
	}
}
