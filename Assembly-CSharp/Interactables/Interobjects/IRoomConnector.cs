using System;
using MapGeneration;
using UnityEngine;

namespace Interactables.Interobjects
{
	public interface IRoomConnector
	{
		RoomIdentifier[] Rooms { get; }

		event Action OnRoomsRegistered;

		bool RoomsAlreadyRegistered { get; }

		bool IsVisibleThrough { get; }

		protected static readonly Vector3[] WorldDirections = new Vector3[]
		{
			Vector3.forward,
			Vector3.back,
			Vector3.left,
			Vector3.right
		};

		protected static readonly RoomIdentifier[] RoomsNonAlloc = new RoomIdentifier[IRoomConnector.WorldDirections.Length];
	}
}
