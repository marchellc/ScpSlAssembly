using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace MapGeneration.RoomConnectors.Spawners;

public abstract class RoomConnectorSpawnpointBase : MonoBehaviour
{
	private static readonly Queue<RoomConnectorSpawnpointBase> AllInstances = new Queue<RoomConnectorSpawnpointBase>();

	private const float MinSqrDistanceBetweenConnectors = 1f;

	private RoomIdentifier _parentRoom;

	public string DesiredNametag;

	public int ConnectorPriority;

	public int SpawnPriority;

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
		if (RoomConnectorDistributorSettings.TryGetTemplate(type, out var result))
		{
			SpawnableRoomConnector spawnableRoomConnector = Object.Instantiate(result, base.transform.position, base.transform.rotation);
			if (spawnableRoomConnector.TryGetComponent<DoorVariant>(out var component) && !string.IsNullOrEmpty(this.DesiredNametag))
			{
				component.gameObject.AddComponent<DoorNametagExtension>().UpdateName(this.DesiredNametag);
			}
			NetworkServer.Spawn(spawnableRoomConnector.gameObject);
		}
	}

	protected abstract void MergeWith(RoomConnectorSpawnpointBase retired);

	protected virtual void OnRetire(RoomConnectorSpawnpointBase successor)
	{
	}

	public static void SetupAllRoomConnectors()
	{
		HashSet<RoomConnectorSpawnpointBase> readyToSpawn = RoomConnectorSpawnpointBase.ResolveConnectorConflicts();
		if (NetworkServer.active)
		{
			RoomConnectorSpawner.ServerSpawnAllConnectors(readyToSpawn);
		}
	}

	private static HashSet<RoomConnectorSpawnpointBase> ResolveConnectorConflicts()
	{
		HashSet<RoomConnectorSpawnpointBase> hashSet = new HashSet<RoomConnectorSpawnpointBase>();
		while (RoomConnectorSpawnpointBase.AllInstances.Count > 0)
		{
			RoomConnectorSpawnpointBase roomConnectorSpawnpointBase = RoomConnectorSpawnpointBase.AllInstances.Dequeue();
			if (roomConnectorSpawnpointBase == null || !roomConnectorSpawnpointBase.gameObject.activeInHierarchy)
			{
				continue;
			}
			Vector3 position = roomConnectorSpawnpointBase.transform.position;
			bool flag = true;
			RoomConnectorSpawnpointBase roomConnectorSpawnpointBase2 = null;
			foreach (RoomConnectorSpawnpointBase item in hashSet)
			{
				if ((item.transform.position - position).sqrMagnitude > 1f)
				{
					continue;
				}
				RoomConnectorSpawnpointBase.ConnectRooms(item, roomConnectorSpawnpointBase);
				if (NetworkServer.active)
				{
					if (roomConnectorSpawnpointBase.ConnectorPriority > item.ConnectorPriority)
					{
						RoomConnectorSpawnpointBase.MergeRoomConnectors(roomConnectorSpawnpointBase, item);
						roomConnectorSpawnpointBase2 = item;
					}
					else
					{
						RoomConnectorSpawnpointBase.MergeRoomConnectors(item, roomConnectorSpawnpointBase);
						flag = false;
					}
					break;
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
		return hashSet;
	}

	private static void MergeRoomConnectors(RoomConnectorSpawnpointBase higherPriority, RoomConnectorSpawnpointBase lowerPriority)
	{
		Vector3 position = higherPriority.transform.position;
		Vector3 position2 = lowerPriority.transform.position;
		Vector3 position3 = (position + position2) / 2f;
		higherPriority.transform.position = position3;
		higherPriority.MergeWith(lowerPriority);
		lowerPriority.OnRetire(higherPriority);
		if (!string.IsNullOrEmpty(lowerPriority.DesiredNametag))
		{
			higherPriority.DesiredNametag = lowerPriority.DesiredNametag;
		}
	}

	private static void ConnectRooms(RoomConnectorSpawnpointBase connector1, RoomConnectorSpawnpointBase connector2)
	{
		if (!(connector1._parentRoom == connector2._parentRoom))
		{
			connector1._parentRoom.ConnectedRooms.Add(connector2._parentRoom);
			connector2._parentRoom.ConnectedRooms.Add(connector1._parentRoom);
			if (!NetworkServer.active)
			{
				Object.Destroy(connector2.gameObject);
				Object.Destroy(connector1.gameObject);
			}
		}
	}
}
