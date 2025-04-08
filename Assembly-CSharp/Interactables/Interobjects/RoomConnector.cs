using System;
using System.Collections.Generic;
using MapGeneration;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class RoomConnector : MonoBehaviour, IRoomConnector
	{
		public bool RoomsAlreadyRegistered { get; private set; }

		public RoomIdentifier[] Rooms { get; private set; }

		public event Action OnRoomsRegistered;

		public bool IsVisibleThrough
		{
			get
			{
				return true;
			}
		}

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
				Vector3Int vector3Int = RoomUtils.PositionToCoords(position + IRoomConnector.WorldDirections[i]);
				RoomIdentifier roomIdentifier;
				if (RoomIdentifier.RoomsByCoordinates.TryGetValue(vector3Int, out roomIdentifier) && hashSet.Add(roomIdentifier))
				{
					IRoomConnector.RoomsNonAlloc[num] = roomIdentifier;
					num++;
				}
			}
			HashSetPool<RoomIdentifier>.Shared.Return(hashSet);
			this.Rooms = new RoomIdentifier[num];
			Array.Copy(IRoomConnector.RoomsNonAlloc, this.Rooms, num);
			Action onRoomsRegistered = this.OnRoomsRegistered;
			if (onRoomsRegistered != null)
			{
				onRoomsRegistered();
			}
			this.RoomsAlreadyRegistered = true;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationStage += RoomConnector.OnMapStage;
		}

		private static void OnMapStage(MapGenerationPhase stage)
		{
			if (stage != MapGenerationPhase.ParentRoomRegistration)
			{
				return;
			}
			foreach (RoomConnector roomConnector in RoomConnector.AllConnectors)
			{
				roomConnector.RegisterRooms();
			}
		}

		public static readonly HashSet<RoomConnector> AllConnectors = new HashSet<RoomConnector>();
	}
}
