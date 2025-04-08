using System;
using System.Collections.Generic;
using System.Linq;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public class Scp244TransferCondition
	{
		private Scp244TransferCondition(Bounds b, DoorVariant[] dv, Scp244DeployablePickup scp)
		{
			Vector3 position = scp.transform.position;
			this.ClosestPoint = Vector3.Distance(position, b.ClosestPoint(position));
			this.BoundsToEncapsulate = b;
			this.Doors = dv ?? new DoorVariant[0];
		}

		public static Scp244TransferCondition[] GenerateTransferConditions(Scp244DeployablePickup scp244)
		{
			List<Scp244TransferCondition> list = new List<Scp244TransferCondition>();
			Vector3Int vector3Int = RoomUtils.PositionToCoords(scp244.transform.position);
			RoomIdentifier roomIdentifier;
			if (!RoomIdentifier.RoomsByCoordinates.TryGetValue(vector3Int, out roomIdentifier) || roomIdentifier == null)
			{
				Bounds bounds = new Bounds(scp244.transform.position, Vector3.one * scp244.MaxDiameter * 2f);
				list.Add(new Scp244TransferCondition(bounds, null, scp244));
				return list.ToArray();
			}
			List<Bounds> list2 = new List<Bounds>();
			HashSet<Vector3> hashSet = new HashSet<Vector3>();
			Vector3 position = scp244.transform.position;
			Bounds[] array = roomIdentifier.SubBounds ?? new Bounds[0];
			Bounds bounds2 = Scp244TransferCondition.GetBoundsOfEntireRoom(roomIdentifier);
			HashSet<DoorVariant> hashSet2 = new HashSet<DoorVariant>();
			float num = float.MaxValue;
			foreach (Bounds bounds3 in array)
			{
				Bounds relativeBounds = Scp244TransferCondition.GetRelativeBounds(roomIdentifier.transform, bounds3);
				list2.Add(relativeBounds);
				float sqrMagnitude = (relativeBounds.ClosestPoint(position) - position).sqrMagnitude;
				if (sqrMagnitude <= num)
				{
					bounds2 = relativeBounds;
					num = sqrMagnitude;
				}
			}
			if (list2.Count == 0)
			{
				list2.Add(bounds2);
			}
			List<Vector3Int> list3 = new List<Vector3Int>();
			Scp244TransferCondition.AddNearbyRooms(ref list3, vector3Int, roomIdentifier);
			foreach (Vector3Int vector3Int2 in list3)
			{
				RoomIdentifier roomIdentifier2;
				if (RoomIdentifier.RoomsByCoordinates.TryGetValue(vector3Int2, out roomIdentifier2) && !(roomIdentifier2 == null))
				{
					Scp244TransferCondition.HandleRoomBounds(ref list2, roomIdentifier2);
				}
			}
			foreach (Bounds bounds4 in list2)
			{
				if (hashSet.Add(bounds4.center))
				{
					Bounds bounds5 = new Bounds(bounds2.center, Scp244TransferCondition.DoorDetectionThickness);
					bounds5.Encapsulate(bounds4.center);
					Scp244TransferCondition.AddDoorsFromRoom(ref hashSet2, bounds5, bounds2.center, list2);
					Scp244TransferCondition.AddDoorsFromRoom(ref hashSet2, bounds5, bounds4.center, list2);
					list.Add(new Scp244TransferCondition(bounds4, hashSet2.ToArray<DoorVariant>(), scp244));
					hashSet2.Clear();
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].ClosestPoint >= scp244.MaxDiameter)
				{
					list.RemoveAt(j);
					j--;
				}
			}
			return list.ToArray();
		}

		private static Bounds GetBoundsOfEntireRoom(RoomIdentifier rid)
		{
			Bounds bounds = new Bounds(rid.transform.position, Vector3.zero);
			Vector3 gridScale = RoomIdentifier.GridScale;
			foreach (Vector3Int vector3Int in rid.OccupiedCoords)
			{
				bounds.Encapsulate(new Bounds(Vector3.Scale(vector3Int, gridScale), gridScale));
			}
			return bounds;
		}

		private static void HandleRoomBounds(ref List<Bounds> extraBounds, RoomIdentifier rid)
		{
			if (rid.SubBounds == null || rid.SubBounds.Length == 0)
			{
				extraBounds.Add(Scp244TransferCondition.GetBoundsOfEntireRoom(rid));
				return;
			}
			foreach (Bounds bounds in rid.SubBounds)
			{
				extraBounds.Add(Scp244TransferCondition.GetRelativeBounds(rid.transform, bounds));
			}
		}

		private static void AddNearbyRooms(ref List<Vector3Int> nearbyRooms, Vector3Int startCoords, RoomIdentifier startRoom)
		{
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(startRoom, out hashSet))
			{
				return;
			}
			Vector3 position = startRoom.transform.position;
			foreach (DoorVariant doorVariant in hashSet)
			{
				Vector3 vector = doorVariant.transform.position - position;
				Vector3Int vector3Int = RoomUtils.PositionToCoords(position + vector * 1.05f);
				if (vector3Int != startCoords)
				{
					nearbyRooms.Add(vector3Int);
				}
			}
		}

		private static void AddDoorsFromRoom(ref HashSet<DoorVariant> doors, Bounds bounds, Vector3 room, List<Bounds> allBounds)
		{
			RoomIdentifier roomIdentifier;
			if (!RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(room), out roomIdentifier))
			{
				return;
			}
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(roomIdentifier, out hashSet))
			{
				return;
			}
			foreach (DoorVariant doorVariant in hashSet)
			{
				Vector3 position = doorVariant.transform.position;
				if (bounds.Contains(position))
				{
					int num = 0;
					foreach (Bounds bounds2 in allBounds)
					{
						if (bounds2.SqrDistance(position) <= 9f && ++num > 1)
						{
							doors.Add(doorVariant);
							break;
						}
					}
				}
			}
		}

		private static Bounds GetRelativeBounds(Transform relativeTo, Bounds refBounds)
		{
			Vector3 vector = relativeTo.TransformPoint(refBounds.center);
			Vector3 vector2 = relativeTo.rotation * refBounds.size;
			vector2.x = Mathf.Abs(vector2.x);
			vector2.z = Mathf.Abs(vector2.z);
			return new Bounds(vector, vector2);
		}

		private static readonly Vector3 DoorDetectionThickness = new Vector3(3f, 100f, 3f);

		private const float MinimalDoorGapSqrt = 9f;

		private const float BorderDoorCheck = 1.05f;

		public readonly Bounds BoundsToEncapsulate;

		public readonly DoorVariant[] Doors;

		public readonly float ClosestPoint;
	}
}
