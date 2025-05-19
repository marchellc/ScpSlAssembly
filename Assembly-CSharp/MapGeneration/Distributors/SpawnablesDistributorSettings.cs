using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors;

[CreateAssetMenu(fileName = "New Spawner Settings Preset", menuName = "ScriptableObject/Map Generation/Spawnable Elements Settings")]
public class SpawnablesDistributorSettings : ScriptableObject
{
	private static SpawnablesDistributorSettings[] _allSettings;

	[Range(0.05f, 5f)]
	public float SpawnerDelay;

	[Range(0.05f, 5f)]
	public float UnfreezeDelay;

	public SpawnableStructure[] SpawnableStructures;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		_allSettings = Resources.LoadAll<SpawnablesDistributorSettings>(string.Empty);
		CustomNetworkManager.OnClientStarted += RegisterSpawnables;
	}

	private static void RegisterSpawnables()
	{
		SpawnablesDistributorSettings[] allSettings = _allSettings;
		for (int i = 0; i < allSettings.Length; i++)
		{
			SpawnableStructure[] spawnableStructures = allSettings[i].SpawnableStructures;
			for (int j = 0; j < spawnableStructures.Length; j++)
			{
				NetworkIdentity netIdentity = spawnableStructures[j].netIdentity;
				NetworkClient.prefabs[netIdentity.assetId] = netIdentity.gameObject;
			}
		}
	}
}
