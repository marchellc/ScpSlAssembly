using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.Spectating;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;

namespace Respawning;

public static class WaveManager
{
	private const float DefaultPauseDuration = 1f;

	private const float WarheadDetonationTeamTimers = 165f;

	public static readonly List<SpawnableWaveBase> Waves = new List<SpawnableWaveBase>
	{
		new NtfSpawnWave(),
		new ChaosSpawnWave(),
		new NtfMiniWave(),
		new ChaosMiniWave()
	};

	private static SpawnableWaveBase _nextWave;

	public static WaveQueueState State { get; private set; } = WaveQueueState.Idle;

	private static bool IsRestarting => !SeedSynchronizer.MapGenerated;

	public static event Action<SpawnableWaveBase> OnWaveTrigger;

	public static event Action<SpawnableWaveBase, List<ReferenceHub>> OnWaveSpawned;

	public static event Action<WaveUpdateMessage> OnWaveUpdateMsgReceived;

	public static bool TryGet<T>(out T spawnWave)
	{
		foreach (SpawnableWaveBase wave in Waves)
		{
			if (wave is T val)
			{
				spawnWave = val;
				return true;
			}
		}
		spawnWave = default(T);
		return false;
	}

	public static bool TryGet(Faction faction, out SpawnableWaveBase spawnWave)
	{
		foreach (SpawnableWaveBase wave in Waves)
		{
			if (wave.TargetFaction == faction && !(wave is IMiniWave))
			{
				spawnWave = wave;
				return true;
			}
		}
		spawnWave = null;
		return false;
	}

	public static void AdvanceTimer(Faction faction, float time)
	{
		foreach (TimeBasedWave wave in Waves)
		{
			if (wave.TargetFaction == faction && wave.ReceiveObjectiveRewards)
			{
				wave.Timer.AddTime(Mathf.Abs(time));
			}
		}
	}

	public static void InitiateRespawn(SpawnableWaveBase wave)
	{
		if (NetworkServer.active && !IsRestarting)
		{
			WaveTeamSelectingEventArgs waveTeamSelectingEventArgs = new WaveTeamSelectingEventArgs(wave);
			ServerEvents.OnWaveTeamSelecting(waveTeamSelectingEventArgs);
			if (waveTeamSelectingEventArgs.IsAllowed)
			{
				_nextWave = waveTeamSelectingEventArgs.Wave;
				State = WaveQueueState.WaveSelected;
				ServerEvents.OnWaveTeamSelected(new WaveTeamSelectedEventArgs(_nextWave));
			}
		}
	}

	public static void Spawn(SpawnableWaveBase wave)
	{
		State = WaveQueueState.WaveSpawned;
		_nextWave = null;
		wave.OnWaveSpawned();
		List<ReferenceHub> list = WaveSpawner.SpawnWave(wave);
		WaveManager.OnWaveSpawned?.Invoke(wave, list);
		ListPool<ReferenceHub>.Shared.Return(list);
		State = WaveQueueState.Idle;
	}

	private static void Update()
	{
		if (!NetworkServer.active || IsRestarting)
		{
			return;
		}
		if (State != 0)
		{
			RefreshNextWave();
			return;
		}
		foreach (TimeBasedWave wave in Waves)
		{
			if (wave.Configuration.IsEnabled && wave.IsReadyToSpawn && !wave.Timer.IsPaused && !(wave is ILimitedWave { RespawnTokens: <=0 }))
			{
				InitiateRespawn(wave);
				break;
			}
		}
	}

	private static void RefreshNextWave()
	{
		if (WaveSpawner.AnyPlayersAvailable)
		{
			if (State != WaveQueueState.WaveSpawning)
			{
				State = WaveQueueState.WaveSpawning;
				WaveManager.OnWaveTrigger?.Invoke(_nextWave);
			}
			if (!(_nextWave is IAnimatedWave { IsAnimationPlaying: not false }))
			{
				Spawn(_nextWave);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		AlphaWarheadController.OnDetonated += OnWarheadDetonate;
		StaticUnityMethods.OnUpdate += Update;
		ReferenceHub.OnPlayerAdded += SyncValues;
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<WaveUpdateMessage>(ClientMessageReceived);
			State = WaveQueueState.Idle;
		};
		PlayerRoleManager.OnServerRoleSet += delegate(ReferenceHub hub, RoleTypeId role, RoleChangeReason reason)
		{
			if (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var result) && result is SpectatorRole)
			{
				SyncValues(hub);
			}
		};
		OnWaveTrigger += delegate(SpawnableWaveBase w)
		{
			float duration = ((w is IAnimatedWave animatedWave) ? animatedWave.AnimationDuration : 1f);
			foreach (TimeBasedWave wave in Waves)
			{
				wave.Timer.Pause(duration);
			}
			if (NetworkServer.active)
			{
				WaveUpdateMessage.ServerSendUpdate(_nextWave, UpdateMessageFlags.Trigger);
			}
		};
	}

	private static void OnWarheadDetonate()
	{
		foreach (TimeBasedWave wave in Waves)
		{
			wave.Timer.SpawnIntervalSeconds = 165f;
			wave.Timer.Reset(resetSpawnInterval: false);
		}
	}

	private static void SyncValues(ReferenceHub hub)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (TimeBasedWave wave in Waves)
		{
			UpdateMessageFlags updateMessageFlags = UpdateMessageFlags.All;
			if (!wave.Timer.IsPaused)
			{
				updateMessageFlags &= ~UpdateMessageFlags.Pause;
			}
			WaveUpdateMessage.ServerSendUpdate(wave, updateMessageFlags);
		}
		foreach (KeyValuePair<Faction, float> item in FactionInfluenceManager.Influence)
		{
			hub.connectionToClient.Send(new InfluenceUpdateMessage
			{
				Faction = item.Key,
				Influence = item.Value
			});
		}
	}

	private static void ClientMessageReceived(WaveUpdateMessage msg)
	{
		WaveManager.OnWaveUpdateMsgReceived?.Invoke(msg);
		if (!NetworkServer.active && msg.IsTrigger)
		{
			WaveManager.OnWaveTrigger?.Invoke(msg.Wave);
		}
	}
}
