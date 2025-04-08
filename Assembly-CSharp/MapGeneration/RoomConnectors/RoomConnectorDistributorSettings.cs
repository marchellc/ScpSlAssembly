using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace MapGeneration.RoomConnectors
{
	[CreateAssetMenu(fileName = "New Connector Distributor Settings Preset", menuName = "ScriptableObject/Map Generation/Connector Distributor Settings")]
	public class RoomConnectorDistributorSettings : ScriptableObject
	{
		public static bool TryGetTemplate(SpawnableRoomConnectorType type, out SpawnableRoomConnector result)
		{
			foreach (SpawnableRoomConnector spawnableRoomConnector in RoomConnectorDistributorSettings.RegisteredConnectors)
			{
				if (spawnableRoomConnector.SpawnData.ConnectorType == type)
				{
					result = spawnableRoomConnector;
					return true;
				}
			}
			result = null;
			return false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			RoomConnectorDistributorSettings.LoadSettingsFromResources();
			CustomNetworkManager.OnClientStarted += RoomConnectorDistributorSettings.RegisterSpawnables;
		}

		private static void LoadSettingsFromResources()
		{
			RoomConnectorDistributorSettings[] array = Resources.LoadAll<RoomConnectorDistributorSettings>(string.Empty);
			RoomConnectorDistributorSettings.RegisteredConnectors.Clear();
			RoomConnectorDistributorSettings[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].RegisterAll();
			}
		}

		private static void RegisterSpawnables()
		{
			foreach (SpawnableRoomConnector spawnableRoomConnector in RoomConnectorDistributorSettings.RegisteredConnectors)
			{
				NetworkIdentity netIdentity = spawnableRoomConnector.netIdentity;
				NetworkClient.prefabs[netIdentity.assetId] = netIdentity.gameObject;
			}
		}

		private void RegisterAll()
		{
			foreach (SpawnableRoomConnector spawnableRoomConnector in this.SpawnableConnectors)
			{
				RoomConnectorDistributorSettings.RegisteredConnectors.AddIfNotContains(spawnableRoomConnector);
			}
		}

		public static readonly List<SpawnableRoomConnector> RegisteredConnectors = new List<SpawnableRoomConnector>();

		public SpawnableRoomConnector[] SpawnableConnectors;
	}
}
