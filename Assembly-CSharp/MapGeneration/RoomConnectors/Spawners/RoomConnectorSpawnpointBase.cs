using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace MapGeneration.RoomConnectors.Spawners
{
	public abstract class RoomConnectorSpawnpointBase : MonoBehaviour
	{
		public abstract SpawnableRoomConnectorType FallbackType { get; }

		protected virtual void Awake()
		{
			this._parentRoom = base.GetComponentInParent<RoomIdentifier>();
			RoomConnectorSpawnpointBase.AllInstances.Enqueue(this);
		}

		public abstract float GetSpawnChanceWeight(SpawnableRoomConnectorType type);

		public virtual void SpawnFallback()
		{
			this.Spawn(this.FallbackType);
		}

		public void Spawn(SpawnableRoomConnectorType type)
		{
			SpawnableRoomConnector spawnableRoomConnector;
			if (!RoomConnectorDistributorSettings.TryGetTemplate(type, out spawnableRoomConnector))
			{
				return;
			}
			SpawnableRoomConnector spawnableRoomConnector2 = global::UnityEngine.Object.Instantiate<SpawnableRoomConnector>(spawnableRoomConnector, base.transform.position, base.transform.rotation);
			DoorVariant doorVariant;
			if (spawnableRoomConnector2.TryGetComponent<DoorVariant>(out doorVariant) && !string.IsNullOrEmpty(this.DesiredNametag))
			{
				doorVariant.gameObject.AddComponent<DoorNametagExtension>().UpdateName(this.DesiredNametag);
			}
			NetworkServer.Spawn(spawnableRoomConnector2.gameObject, null);
		}

		protected abstract void MergeWith(RoomConnectorSpawnpointBase retired);

		protected virtual void OnRetire(RoomConnectorSpawnpointBase successor)
		{
		}

		public static void SetupAllRoomConnectors()
		{
			HashSet<RoomConnectorSpawnpointBase> hashSet = RoomConnectorSpawnpointBase.ResolveConnectorConflicts();
			if (!NetworkServer.active)
			{
				return;
			}
			RoomConnectorSpawner.ServerSpawnAllConnectors(hashSet);
		}

		private static HashSet<RoomConnectorSpawnpointBase> ResolveConnectorConflicts()
		{
			HashSet<RoomConnectorSpawnpointBase> hashSet = new HashSet<RoomConnectorSpawnpointBase>();
			while (RoomConnectorSpawnpointBase.AllInstances.Count > 0)
			{
				RoomConnectorSpawnpointBase roomConnectorSpawnpointBase = RoomConnectorSpawnpointBase.AllInstances.Dequeue();
				if (!(roomConnectorSpawnpointBase == null) && roomConnectorSpawnpointBase.gameObject.activeInHierarchy)
				{
					Vector3 position = roomConnectorSpawnpointBase.transform.position;
					bool flag = true;
					RoomConnectorSpawnpointBase roomConnectorSpawnpointBase2 = null;
					foreach (RoomConnectorSpawnpointBase roomConnectorSpawnpointBase3 in hashSet)
					{
						if ((roomConnectorSpawnpointBase3.transform.position - position).sqrMagnitude <= 1f)
						{
							RoomConnectorSpawnpointBase.ConnectRooms(roomConnectorSpawnpointBase3, roomConnectorSpawnpointBase);
							if (NetworkServer.active)
							{
								if (roomConnectorSpawnpointBase.ConnectorPriority > roomConnectorSpawnpointBase3.ConnectorPriority)
								{
									RoomConnectorSpawnpointBase.MergeRoomConnectors(roomConnectorSpawnpointBase, roomConnectorSpawnpointBase3);
									roomConnectorSpawnpointBase2 = roomConnectorSpawnpointBase3;
									break;
								}
								RoomConnectorSpawnpointBase.MergeRoomConnectors(roomConnectorSpawnpointBase3, roomConnectorSpawnpointBase);
								flag = false;
								break;
							}
						}
					}
					if (flag)
					{
						hashSet.Add(roomConnectorSpawnpointBase);
					}
					if (roomConnectorSpawnpointBase2 != null)
					{
						hashSet.Remove(roomConnectorSpawnpointBase2);
					}
				}
			}
			return hashSet;
		}

		private static void MergeRoomConnectors(RoomConnectorSpawnpointBase higherPriority, RoomConnectorSpawnpointBase lowerPriority)
		{
			Vector3 position = higherPriority.transform.position;
			Vector3 position2 = lowerPriority.transform.position;
			Vector3 vector = (position + position2) / 2f;
			higherPriority.transform.position = vector;
			higherPriority.MergeWith(lowerPriority);
			lowerPriority.OnRetire(higherPriority);
			if (!string.IsNullOrEmpty(lowerPriority.DesiredNametag))
			{
				higherPriority.DesiredNametag = lowerPriority.DesiredNametag;
			}
		}

		private static void ConnectRooms(RoomConnectorSpawnpointBase connector1, RoomConnectorSpawnpointBase connector2)
		{
			if (connector1._parentRoom == connector2._parentRoom)
			{
				return;
			}
			connector1._parentRoom.ConnectedRooms.Add(connector2._parentRoom);
			connector2._parentRoom.ConnectedRooms.Add(connector1._parentRoom);
			if (NetworkServer.active)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(connector2.gameObject);
			global::UnityEngine.Object.Destroy(connector1.gameObject);
		}

		private static readonly Queue<RoomConnectorSpawnpointBase> AllInstances = new Queue<RoomConnectorSpawnpointBase>();

		private const float MinSqrDistanceBetweenConnectors = 1f;

		private RoomIdentifier _parentRoom;

		public string DesiredNametag;

		public int ConnectorPriority;

		public int SpawnPriority;
	}
}
