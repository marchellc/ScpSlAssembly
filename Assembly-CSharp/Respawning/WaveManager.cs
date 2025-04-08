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

namespace Respawning
{
	public static class WaveManager
	{
		public static event Action<SpawnableWaveBase> OnWaveTrigger;

		public static event Action<SpawnableWaveBase, List<ReferenceHub>> OnWaveSpawned;

		public static event Action<WaveUpdateMessage> OnWaveUpdateMsgReceived;

		public static WaveQueueState State { get; private set; } = WaveQueueState.Idle;

		private static bool IsRestarting
		{
			get
			{
				return !SeedSynchronizer.MapGenerated;
			}
		}

		public static bool TryGet<T>(out T spawnWave)
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				if (spawnableWaveBase is T)
				{
					T t = spawnableWaveBase as T;
					spawnWave = t;
					return true;
				}
			}
			spawnWave = default(T);
			return false;
		}

		public static bool TryGet(Faction faction, out SpawnableWaveBase spawnWave)
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				if (spawnableWaveBase.TargetFaction == faction && !(spawnableWaveBase is IMiniWave))
				{
					spawnWave = spawnableWaveBase;
					return true;
				}
			}
			spawnWave = null;
			return false;
		}

		public static void AdvanceTimer(Faction faction, float time)
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				if (timeBasedWave.TargetFaction == faction && timeBasedWave.ReceiveObjectiveRewards)
				{
					timeBasedWave.Timer.AddTime(Mathf.Abs(time));
				}
			}
		}

		public static void InitiateRespawn(SpawnableWaveBase wave)
		{
			if (!NetworkServer.active || WaveManager.IsRestarting)
			{
				return;
			}
			WaveTeamSelectingEventArgs waveTeamSelectingEventArgs = new WaveTeamSelectingEventArgs(wave);
			ServerEvents.OnWaveTeamSelecting(waveTeamSelectingEventArgs);
			if (!waveTeamSelectingEventArgs.IsAllowed)
			{
				return;
			}
			WaveManager._nextWave = waveTeamSelectingEventArgs.Wave;
			WaveManager.State = WaveQueueState.WaveSelected;
			ServerEvents.OnWaveTeamSelected(new WaveTeamSelectedEventArgs(WaveManager._nextWave));
		}

		public static void Spawn(SpawnableWaveBase wave)
		{
			WaveManager.State = WaveQueueState.WaveSpawned;
			WaveManager._nextWave = null;
			wave.OnWaveSpawned();
			List<ReferenceHub> list = WaveSpawner.SpawnWave(wave);
			Action<SpawnableWaveBase, List<ReferenceHub>> onWaveSpawned = WaveManager.OnWaveSpawned;
			if (onWaveSpawned != null)
			{
				onWaveSpawned(wave, list);
			}
			ListPool<ReferenceHub>.Shared.Return(list);
			WaveManager.State = WaveQueueState.Idle;
		}

		private static void Update()
		{
			if (!NetworkServer.active || WaveManager.IsRestarting)
			{
				return;
			}
			if (WaveManager.State != WaveQueueState.Idle)
			{
				WaveManager.RefreshNextWave();
				return;
			}
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				if (timeBasedWave.Configuration.IsEnabled && timeBasedWave.IsReadyToSpawn && !timeBasedWave.Timer.IsPaused)
				{
					ILimitedWave limitedWave = timeBasedWave as ILimitedWave;
					if (limitedWave == null || limitedWave.RespawnTokens > 0)
					{
						WaveManager.InitiateRespawn(timeBasedWave);
						break;
					}
				}
			}
		}

		private static void RefreshNextWave()
		{
			if (!WaveSpawner.AnyPlayersAvailable)
			{
				return;
			}
			if (WaveManager.State != WaveQueueState.WaveSpawning)
			{
				WaveManager.State = WaveQueueState.WaveSpawning;
				Action<SpawnableWaveBase> onWaveTrigger = WaveManager.OnWaveTrigger;
				if (onWaveTrigger != null)
				{
					onWaveTrigger(WaveManager._nextWave);
				}
			}
			IAnimatedWave animatedWave = WaveManager._nextWave as IAnimatedWave;
			if (animatedWave != null && animatedWave.IsAnimationPlaying)
			{
				return;
			}
			WaveManager.Spawn(WaveManager._nextWave);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			AlphaWarheadController.OnDetonated += WaveManager.OnWarheadDetonate;
			StaticUnityMethods.OnUpdate += WaveManager.Update;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(WaveManager.SyncValues));
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<WaveUpdateMessage>(new Action<WaveUpdateMessage>(WaveManager.ClientMessageReceived), true);
				WaveManager.State = WaveQueueState.Idle;
			};
			PlayerRoleManager.OnServerRoleSet += delegate(ReferenceHub hub, RoleTypeId role, RoleChangeReason reason)
			{
				PlayerRoleBase playerRoleBase;
				if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
				{
					return;
				}
				if (!(playerRoleBase is SpectatorRole))
				{
					return;
				}
				WaveManager.SyncValues(hub);
			};
			WaveManager.OnWaveTrigger += delegate(SpawnableWaveBase w)
			{
				IAnimatedWave animatedWave = w as IAnimatedWave;
				float num = ((animatedWave != null) ? animatedWave.AnimationDuration : 1f);
				foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
				{
					((TimeBasedWave)spawnableWaveBase).Timer.Pause(num);
				}
				if (!NetworkServer.active)
				{
					return;
				}
				WaveUpdateMessage.ServerSendUpdate(WaveManager._nextWave, UpdateMessageFlags.Trigger);
			};
		}

		private static void OnWarheadDetonate()
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				timeBasedWave.Timer.SpawnIntervalSeconds = 165f;
				timeBasedWave.Timer.Reset(false);
			}
		}

		private static void SyncValues(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				UpdateMessageFlags updateMessageFlags = UpdateMessageFlags.All;
				if (!timeBasedWave.Timer.IsPaused)
				{
					updateMessageFlags &= ~UpdateMessageFlags.Pause;
				}
				WaveUpdateMessage.ServerSendUpdate(timeBasedWave, updateMessageFlags);
			}
			foreach (KeyValuePair<Faction, float> keyValuePair in FactionInfluenceManager.Influence)
			{
				hub.connectionToClient.Send<InfluenceUpdateMessage>(new InfluenceUpdateMessage
				{
					Faction = keyValuePair.Key,
					Influence = keyValuePair.Value
				}, 0);
			}
		}

		private static void ClientMessageReceived(WaveUpdateMessage msg)
		{
			Action<WaveUpdateMessage> onWaveUpdateMsgReceived = WaveManager.OnWaveUpdateMsgReceived;
			if (onWaveUpdateMsgReceived != null)
			{
				onWaveUpdateMsgReceived(msg);
			}
			if (NetworkServer.active || !msg.IsTrigger)
			{
				return;
			}
			Action<SpawnableWaveBase> onWaveTrigger = WaveManager.OnWaveTrigger;
			if (onWaveTrigger == null)
			{
				return;
			}
			onWaveTrigger(msg.Wave);
		}

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
	}
}
