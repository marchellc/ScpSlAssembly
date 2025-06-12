using System;
using System.Diagnostics;
using GameCore;
using GameObjectPools;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration.RoomConnectors.Spawners;
using Mirror;
using UnityEngine;

namespace MapGeneration;

[DefaultExecutionOrder(1)]
public class SeedSynchronizer : MonoBehaviour
{
	public struct SeedMessage : NetworkMessage
	{
		public int Value;
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

	public static float TimeSinceMapGeneration => (float)SeedSynchronizer.MapgenSw.Elapsed.TotalSeconds;

	public static int Seed { get; private set; }

	public static event Action OnBeforeGenerated;

	public static event Action<MapGenerationPhase> OnGenerationStage;

	public static event Action OnGenerationFinished;

	private void Awake()
	{
		SeedSynchronizer._singleton = this;
		NetworkServer.OnConnectedEvent = (Action<NetworkConnectionToClient>)Delegate.Combine(NetworkServer.OnConnectedEvent, new Action<NetworkConnectionToClient>(OnNewPlayerConnected));
		if (!NetworkServer.active)
		{
			if (SeedSynchronizer._seedTokenValid && !SeedSynchronizer.MapGenerated)
			{
				this.GenerateLevel(SeedSynchronizer._seedTokenValid);
			}
			return;
		}
		int num = ConfigFile.ServerConfig.GetInt("map_seed", -1);
		if (num < 1)
		{
			SeedSynchronizer.Seed = UnityEngine.Random.Range(1, int.MaxValue);
			SeedSynchronizer.DebugInfo("Server has successfully generated a random seed: " + SeedSynchronizer.Seed, MessageImportance.Normal);
		}
		else
		{
			SeedSynchronizer.Seed = Mathf.Clamp(num, 1, int.MaxValue);
			SeedSynchronizer.DebugInfo("Server has successfully loaded a seed from config: " + SeedSynchronizer.Seed, MessageImportance.Normal);
		}
		MapGeneratingEventArgs e = new MapGeneratingEventArgs(SeedSynchronizer.Seed);
		ServerEvents.OnMapGenerating(e);
		if (!e.IsAllowed || e.Seed == -1)
		{
			SeedSynchronizer.DebugInfo("Map generation cancelled by a plugin.", MessageImportance.Normal);
			SeedSynchronizer.Seed = -1;
		}
		else
		{
			SeedSynchronizer.Seed = e.Seed;
		}
		NetworkServer.SendToAll(new SeedMessage
		{
			Value = SeedSynchronizer.Seed
		});
		this.GenerateLevel(SeedSynchronizer.Seed != -1);
	}

	private void OnDestroy()
	{
		SeedSynchronizer.MapGenerated = false;
		NetworkServer.OnConnectedEvent = (Action<NetworkConnectionToClient>)Delegate.Remove(NetworkServer.OnConnectedEvent, new Action<NetworkConnectionToClient>(OnNewPlayerConnected));
	}

	private void GenerateLevel(bool generateFacility)
	{
		if (generateFacility)
		{
			this.GenerateFacility();
		}
		RoomConnectorSpawnpointBase.SetupAllRoomConnectors();
		ServerEvents.OnMapGenerated(new MapGeneratedEventArgs(SeedSynchronizer.Seed));
		if (NetworkServer.active)
		{
			PoolManager.Singleton.RestartRound();
		}
		MapGenerationPhase[] values = EnumUtils<MapGenerationPhase>.Values;
		foreach (MapGenerationPhase obj in values)
		{
			SeedSynchronizer.OnGenerationStage?.Invoke(obj);
		}
		SeedSynchronizer.MapGenerated = true;
		SeedSynchronizer.MapgenSw.Restart();
		SeedSynchronizer.OnGenerationFinished?.Invoke();
	}

	private void GenerateFacility()
	{
		SeedSynchronizer.OnBeforeGenerated?.Invoke();
		System.Random rng = new System.Random(SeedSynchronizer.Seed);
		SeedSynchronizer._seedTokenValid = false;
		for (int i = 0; i < this._zoneGenerators.Length; i++)
		{
			try
			{
				this._zoneGenerators[i].Generate(rng);
			}
			catch (Exception exception)
			{
				UnityEngine.Debug.LogError("Map generation failed at " + this._zoneGenerators[i].TargetZone);
				UnityEngine.Debug.LogException(exception);
			}
		}
		SeedSynchronizer.DebugInfo("Sequence of procedural level generation completed.", MessageImportance.Normal);
	}

	private void OnNewPlayerConnected(NetworkConnectionToClient connectionToClient)
	{
		if (connectionToClient != NetworkClient.connection)
		{
			connectionToClient.Send(new SeedMessage
			{
				Value = SeedSynchronizer.Seed
			});
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientStarted += delegate
		{
			NetworkClient.ReplaceHandler<SeedMessage>(ClientReceiveSeed);
		};
	}

	private static void ClientReceiveSeed(SeedMessage msg)
	{
		if (!NetworkServer.active)
		{
			SeedSynchronizer.Seed = msg.Value;
			SeedSynchronizer._seedTokenValid = SeedSynchronizer.Seed != -1;
			if (!SeedSynchronizer.MapGenerated && SeedSynchronizer._singleton != null)
			{
				SeedSynchronizer._singleton.GenerateLevel(SeedSynchronizer._seedTokenValid);
			}
		}
	}

	internal static void DebugInfo(string txt, MessageImportance importance, bool nospace = false)
	{
		GameCore.Console.AddDebugLog("MAPGEN", txt, importance, nospace);
	}

	internal static void DebugError(bool isFatal, string txt)
	{
		SeedSynchronizer.DebugInfo(string.Format(isFatal ? "<color=red>Fatal Error:</color> {0}" : "<color=orange>Warning:</color> {0}", txt), MessageImportance.MostImportant);
		UnityEngine.Debug.LogError("Map generation error for seed " + SeedSynchronizer.Seed + ": " + txt);
	}
}
