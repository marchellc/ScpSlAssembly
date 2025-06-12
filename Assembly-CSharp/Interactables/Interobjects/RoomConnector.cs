using System;
using System.Collections.Generic;
using MapGeneration;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Interactables.Interobjects;

public class RoomConnector : MonoBehaviour, IRoomConnector
{
	public static readonly HashSet<RoomConnector> AllConnectors = new HashSet<RoomConnector>();

	public bool RoomsAlreadyRegistered { get; private set; }

	public RoomIdentifier[] Rooms { get; private set; }

	public bool IsVisibleThrough => true;

	public event Action OnRoomsRegistered;

	private void Start()
	{
		RoomConnector.AllConnectors.Add(this);
		if (SeedSynchronizer.MapGenerated)
		{
			this.RegisterRooms();
		}
	}

	private void OnDestroy()
	{
		RoomConnector.AllConnectors.Remove(this);
	}

	private void RegisterRooms()
	{
		Vector3 position = base.transform.position;
		int num = 0;
		HashSet<RoomIdentifier> hashSet = HashSetPool<RoomIdentifier>.Shared.Rent();
		for (int i = 0; i < IRoomConnector.WorldDirections.Length; i++)
		{
			Vector3Int key = RoomUtils.PositionToCoords(position + IRoomConnector.WorldDirections[i]);
			if (RoomIdentifier.RoomsByCoords.TryGetValue(key, out var value) && hashSet.Add(value))
			{
				IRoomConnector.RoomsNonAlloc[num] = value;
				num++;
			}
		}
		HashSetPool<RoomIdentifier>.Shared.Return(hashSet);
		this.Rooms = new RoomIdentifier[num];
		Array.Copy(IRoomConnector.RoomsNonAlloc, this.Rooms, num);
		this.OnRoomsRegistered?.Invoke();
		this.RoomsAlreadyRegistered = true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationStage += OnMapStage;
	}

	private static void OnMapStage(MapGenerationPhase stage)
	{
		if (stage != MapGenerationPhase.ParentRoomRegistration)
		{
			return;
		}
		foreach (RoomConnector allConnector in RoomConnector.AllConnectors)
		{
			allConnector.RegisterRooms();
		}
	}
}
