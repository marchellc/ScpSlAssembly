using System;
using System.Collections.Generic;

namespace MapGeneration.RoomConnectors.Spawners
{
	public static class RoomConnectorSpawner
	{
		public static void ServerSpawnAllConnectors(HashSet<RoomConnectorSpawnpointBase> readyToSpawn)
		{
			Random random = new Random(SeedSynchronizer.Seed);
			Queue<SpawnableRoomConnectorType> queue = RoomConnectorSpawner.GenerateSpawnQueue(RoomConnectorSpawner.GenerateCompatibleSpawnpointCounts(readyToSpawn), random);
			List<int> allPriorities = RoomConnectorSpawner.GetAllPriorities(readyToSpawn);
			SpawnableRoomConnectorType spawnableRoomConnectorType;
			while (queue.TryDequeue(out spawnableRoomConnectorType))
			{
				RoomConnectorSpawner.TrySpawnConnector(spawnableRoomConnectorType, allPriorities, readyToSpawn, random);
			}
			foreach (RoomConnectorSpawnpointBase roomConnectorSpawnpointBase in readyToSpawn)
			{
				roomConnectorSpawnpointBase.SpawnFallback();
			}
		}

		private static bool TrySpawnConnector(SpawnableRoomConnectorType type, List<int> priorities, HashSet<RoomConnectorSpawnpointBase> remainingSpawnpoints, Random rng)
		{
			bool flag = false;
			RoomConnectorSpawnpointBase roomConnectorSpawnpointBase = null;
			foreach (int num in priorities)
			{
				if (RoomConnectorSpawner.TryGetRandomSpawnpointForPriority(type, num, remainingSpawnpoints, rng, out roomConnectorSpawnpointBase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			roomConnectorSpawnpointBase.Spawn(type);
			remainingSpawnpoints.Remove(roomConnectorSpawnpointBase);
			return true;
		}

		private static bool TryGetRandomSpawnpointForPriority(SpawnableRoomConnectorType type, int priority, HashSet<RoomConnectorSpawnpointBase> remainingSpawnpoints, Random rng, out RoomConnectorSpawnpointBase result)
		{
			RoomConnectorSpawner.CompatibleNonAlloc.Clear();
			double num = 0.0;
			foreach (RoomConnectorSpawnpointBase roomConnectorSpawnpointBase in remainingSpawnpoints)
			{
				if (roomConnectorSpawnpointBase.SpawnPriority == priority)
				{
					RoomConnectorSpawner.SpawnpointWeightPair spawnpointWeightPair = new RoomConnectorSpawner.SpawnpointWeightPair(roomConnectorSpawnpointBase, type);
					if (spawnpointWeightPair.Weight > 0f)
					{
						num += (double)spawnpointWeightPair.Weight;
						RoomConnectorSpawner.CompatibleNonAlloc.Add(spawnpointWeightPair);
					}
				}
			}
			if (RoomConnectorSpawner.CompatibleNonAlloc.Count == 0)
			{
				result = null;
				return false;
			}
			double num2 = rng.NextDouble() * num;
			double num3 = 0.0;
			foreach (RoomConnectorSpawner.SpawnpointWeightPair spawnpointWeightPair2 in RoomConnectorSpawner.CompatibleNonAlloc)
			{
				num3 += (double)spawnpointWeightPair2.Weight;
				if (num2 <= num3)
				{
					result = spawnpointWeightPair2.Spawnpoint;
					return true;
				}
			}
			result = null;
			return false;
		}

		private static List<int> GetAllPriorities(HashSet<RoomConnectorSpawnpointBase> connectors)
		{
			List<int> list = new List<int>();
			foreach (RoomConnectorSpawnpointBase roomConnectorSpawnpointBase in connectors)
			{
				int spawnPriority = roomConnectorSpawnpointBase.SpawnPriority;
				if (!list.Contains(spawnPriority))
				{
					bool flag = false;
					for (int i = 0; i < list.Count; i++)
					{
						if (spawnPriority > list[i])
						{
							list.Insert(i, spawnPriority);
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						list.Add(spawnPriority);
					}
				}
			}
			return list;
		}

		private static Queue<SpawnableRoomConnectorType> GenerateSpawnQueue(Dictionary<SpawnableRoomConnectorType, int> spawnpointCounts, Random rng)
		{
			Queue<SpawnableRoomConnectorType> queue = new Queue<SpawnableRoomConnectorType>();
			foreach (SpawnableRoomConnector spawnableRoomConnector in RoomConnectorDistributorSettings.RegisteredConnectors)
			{
				RoomConnectorSpawnData spawnData = spawnableRoomConnector.SpawnData;
				int valueOrDefault = spawnpointCounts.GetValueOrDefault(spawnData.ConnectorType);
				int required = spawnData.GetRequired(valueOrDefault);
				for (int i = 0; i < required; i++)
				{
					queue.Enqueue(spawnData.ConnectorType);
				}
			}
			List<SpawnableRoomConnectorType> list = new List<SpawnableRoomConnectorType>();
			foreach (SpawnableRoomConnector spawnableRoomConnector2 in RoomConnectorDistributorSettings.RegisteredConnectors)
			{
				RoomConnectorSpawnData spawnData2 = spawnableRoomConnector2.SpawnData;
				int valueOrDefault2 = spawnpointCounts.GetValueOrDefault(spawnData2.ConnectorType);
				int optional = spawnData2.GetOptional(valueOrDefault2);
				for (int j = 0; j < optional; j++)
				{
					list.Add(spawnData2.ConnectorType);
				}
			}
			list.ShuffleList(rng);
			list.ForEach(new Action<SpawnableRoomConnectorType>(queue.Enqueue));
			return queue;
		}

		private static Dictionary<SpawnableRoomConnectorType, int> GenerateCompatibleSpawnpointCounts(HashSet<RoomConnectorSpawnpointBase> allSpawnpoints)
		{
			Dictionary<SpawnableRoomConnectorType, int> dictionary = new Dictionary<SpawnableRoomConnectorType, int>();
			foreach (RoomConnectorSpawnpointBase roomConnectorSpawnpointBase in allSpawnpoints)
			{
				foreach (SpawnableRoomConnectorType spawnableRoomConnectorType in EnumUtils<SpawnableRoomConnectorType>.Values)
				{
					if (roomConnectorSpawnpointBase.GetSpawnChanceWeight(spawnableRoomConnectorType) > 0f)
					{
						int num;
						if (dictionary.TryGetValue(spawnableRoomConnectorType, out num))
						{
							dictionary[spawnableRoomConnectorType] = num + 1;
						}
						else
						{
							dictionary.Add(spawnableRoomConnectorType, 1);
						}
					}
				}
			}
			return dictionary;
		}

		private static readonly List<RoomConnectorSpawner.SpawnpointWeightPair> CompatibleNonAlloc = new List<RoomConnectorSpawner.SpawnpointWeightPair>();

		private readonly struct SpawnpointWeightPair
		{
			public SpawnpointWeightPair(RoomConnectorSpawnpointBase sp, SpawnableRoomConnectorType type)
			{
				this.Spawnpoint = sp;
				this.Weight = sp.GetSpawnChanceWeight(type);
			}

			public readonly float Weight;

			public readonly RoomConnectorSpawnpointBase Spawnpoint;
		}
	}
}
