using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameCore;
using GameObjectPools;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration.RoomConnectors.Spawners;
using Mirror;
using UnityEngine;

namespace MapGeneration
{
	[DefaultExecutionOrder(1)]
	public class SeedSynchronizer : MonoBehaviour
	{
		public static event Action OnBeforeGenerated;

		public static event Action<MapGenerationPhase> OnGenerationStage;

		public static event Action OnGenerationFinished;

		public static float TimeSinceMapGeneration
		{
			get
			{
				return (float)SeedSynchronizer.MapgenSw.Elapsed.TotalSeconds;
			}
		}

		public static int Seed { get; private set; }

		private void Awake()
		{
			SeedSynchronizer._singleton = this;
			NetworkServer.OnConnectedEvent = (Action<NetworkConnectionToClient>)Delegate.Combine(NetworkServer.OnConnectedEvent, new Action<NetworkConnectionToClient>(this.OnNewPlayerConnected));
			if (!NetworkServer.active)
			{
				if (SeedSynchronizer._seedTokenValid && !SeedSynchronizer.MapGenerated)
				{
					this.GenerateLevel(SeedSynchronizer._seedTokenValid);
				}
				return;
			}
			int @int = ConfigFile.ServerConfig.GetInt("map_seed", -1);
			if (@int < 1)
			{
				SeedSynchronizer.Seed = global::UnityEngine.Random.Range(1, int.MaxValue);
				SeedSynchronizer.DebugInfo("Server has successfully generated a random seed: " + SeedSynchronizer.Seed.ToString(), MessageImportance.Normal, false);
			}
			else
			{
				SeedSynchronizer.Seed = Mathf.Clamp(@int, 1, int.MaxValue);
				SeedSynchronizer.DebugInfo("Server has successfully loaded a seed from config: " + SeedSynchronizer.Seed.ToString(), MessageImportance.Normal, false);
			}
			MapGeneratingEventArgs mapGeneratingEventArgs = new MapGeneratingEventArgs(SeedSynchronizer.Seed);
			ServerEvents.OnMapGenerating(mapGeneratingEventArgs);
			if (!mapGeneratingEventArgs.IsAllowed || mapGeneratingEventArgs.Seed == -1)
			{
				SeedSynchronizer.DebugInfo("Map generation cancelled by a plugin.", MessageImportance.Normal, false);
				SeedSynchronizer.Seed = -1;
			}
			else
			{
				SeedSynchronizer.Seed = mapGeneratingEventArgs.Seed;
			}
			NetworkServer.SendToAll<SeedSynchronizer.SeedMessage>(new SeedSynchronizer.SeedMessage
			{
				Value = SeedSynchronizer.Seed
			}, 0, false);
			this.GenerateLevel(SeedSynchronizer.Seed != -1);
		}

		private void OnDestroy()
		{
			SeedSynchronizer.MapGenerated = false;
			NetworkServer.OnConnectedEvent = (Action<NetworkConnectionToClient>)Delegate.Remove(NetworkServer.OnConnectedEvent, new Action<NetworkConnectionToClient>(this.OnNewPlayerConnected));
		}

		private void GenerateLevel(bool generateFacility)
		{
			if (generateFacility)
			{
				this.GenerateFacility();
			}
			RoomConnectorSpawnpointBase.SetupAllRoomConnectors();
			HashSet<RoomIdentifier> hashSet = new HashSet<RoomIdentifier>();
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if (roomIdentifier == null || !roomIdentifier.TryAssignId())
				{
					hashSet.Add(roomIdentifier);
				}
			}
			foreach (RoomIdentifier roomIdentifier2 in hashSet)
			{
				RoomIdentifier.AllRoomIdentifiers.Remove(roomIdentifier2);
			}
			ServerEvents.OnMapGenerated(new MapGeneratedEventArgs(SeedSynchronizer.Seed));
			if (NetworkServer.active)
			{
				PoolManager.Singleton.RestartRound();
			}
			foreach (MapGenerationPhase mapGenerationPhase in EnumUtils<MapGenerationPhase>.Values)
			{
				Action<MapGenerationPhase> onGenerationStage = SeedSynchronizer.OnGenerationStage;
				if (onGenerationStage != null)
				{
					onGenerationStage(mapGenerationPhase);
				}
			}
			SeedSynchronizer.MapGenerated = true;
			SeedSynchronizer.MapgenSw.Restart();
			Action onGenerationFinished = SeedSynchronizer.OnGenerationFinished;
			if (onGenerationFinished == null)
			{
				return;
			}
			onGenerationFinished();
		}

		private void GenerateFacility()
		{
			Action onBeforeGenerated = SeedSynchronizer.OnBeforeGenerated;
			if (onBeforeGenerated != null)
			{
				onBeforeGenerated();
			}
			global::System.Random random = new global::System.Random(SeedSynchronizer.Seed);
			SeedSynchronizer._seedTokenValid = false;
			for (int i = 0; i < this._zoneGenerators.Length; i++)
			{
				try
				{
					this._zoneGenerators[i].Generate(random);
				}
				catch (Exception ex)
				{
					global::UnityEngine.Debug.LogError("Map generation failed at " + this._zoneGenerators[i].TargetZone.ToString());
					global::UnityEngine.Debug.LogException(ex);
				}
			}
			SeedSynchronizer.DebugInfo("Sequence of procedural level generation completed.", MessageImportance.Normal, false);
		}

		private void OnNewPlayerConnected(NetworkConnectionToClient connectionToClient)
		{
			if (connectionToClient == NetworkClient.connection)
			{
				return;
			}
			connectionToClient.Send<SeedSynchronizer.SeedMessage>(new SeedSynchronizer.SeedMessage
			{
				Value = SeedSynchronizer.Seed
			}, 0);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += delegate
			{
				NetworkClient.ReplaceHandler<SeedSynchronizer.SeedMessage>(new Action<SeedSynchronizer.SeedMessage>(SeedSynchronizer.ClientReceiveSeed), true);
			};
		}

		private static void ClientReceiveSeed(SeedSynchronizer.SeedMessage msg)
		{
			if (NetworkServer.active)
			{
				return;
			}
			SeedSynchronizer.Seed = msg.Value;
			SeedSynchronizer._seedTokenValid = SeedSynchronizer.Seed != -1;
			if (!SeedSynchronizer.MapGenerated && SeedSynchronizer._singleton != null)
			{
				SeedSynchronizer._singleton.GenerateLevel(SeedSynchronizer._seedTokenValid);
			}
		}

		internal static void DebugInfo(string txt, MessageImportance importance, bool nospace = false)
		{
			global::GameCore.Console.AddDebugLog("MAPGEN", txt, importance, nospace);
		}

		internal static void DebugError(bool isFatal, string txt)
		{
			SeedSynchronizer.DebugInfo(string.Format(isFatal ? "<color=red>Fatal Error:</color> {0}" : "<color=orange>Warning:</color> {0}", txt), MessageImportance.MostImportant, false);
			global::UnityEngine.Debug.LogError("Map generation error for seed " + SeedSynchronizer.Seed.ToString() + ": " + txt);
		}

		public const int GenerationDisabledSeed = -1;

		private const string SeedConfigKey = "map_seed";

		private const string DebugLogChannel = "MAPGEN";

		private const string WarningLogFormat = "<color=orange>Warning:</color> {0}";

		private const string ErrorLogFormat = "<color=red>Fatal Error:</color> {0}";

		public static bool MapGenerated;

		[SerializeField]
		private ZoneGenerator[] _zoneGenerators;

		private static readonly Stopwatch MapgenSw = new Stopwatch();

		private static bool _seedTokenValid;

		private static SeedSynchronizer _singleton;

		public struct SeedMessage : NetworkMessage
		{
			public int Value;
		}
	}
}
