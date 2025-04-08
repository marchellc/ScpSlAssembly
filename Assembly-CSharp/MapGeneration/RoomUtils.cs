using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
	public static class RoomUtils
	{
		public static Vector3Int PositionToCoords(Vector3 position)
		{
			Vector3Int zero = Vector3Int.zero;
			for (int i = 0; i < 3; i++)
			{
				zero[i] = Mathf.RoundToInt(position[i] / RoomIdentifier.GridScale[i]);
			}
			return zero;
		}

		public static Vector3 CoordsToCenterPos(Vector3Int position)
		{
			Vector3 zero = Vector3.zero;
			for (int i = 0; i < 3; i++)
			{
				zero[i] = (float)position[i] * RoomIdentifier.GridScale[i];
			}
			return zero;
		}

		public static bool TryFindRoom(RoomName name, FacilityZone zone, RoomShape shape, out RoomIdentifier foundRoom)
		{
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if ((name == RoomName.Unnamed || name == roomIdentifier.Name) && (zone == FacilityZone.None || zone == roomIdentifier.Zone) && (shape == RoomShape.Undefined || shape == roomIdentifier.Shape))
				{
					foundRoom = roomIdentifier;
					return true;
				}
			}
			foundRoom = null;
			return false;
		}

		public static HashSet<RoomIdentifier> FindRooms(RoomName name, FacilityZone zone, RoomShape shape)
		{
			HashSet<RoomIdentifier> hashSet = new HashSet<RoomIdentifier>();
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if ((name == RoomName.Unnamed || name == roomIdentifier.Name) && (zone == FacilityZone.None || zone == roomIdentifier.Zone) && (shape == RoomShape.Undefined || shape == roomIdentifier.Shape))
				{
					hashSet.Add(roomIdentifier);
				}
			}
			return hashSet;
		}

		public static bool IsTheSameRoom(Vector3 pos1, Vector3 pos2)
		{
			return RoomUtils.RoomAtPosition(pos1) == RoomUtils.RoomAtPosition(pos2);
		}

		public static bool IsWithinRoomBoundaries(RoomIdentifier room, Vector3 pos, float extension = 0f, bool accurateMode = false)
		{
			if (accurateMode)
			{
				if (RoomUtils.RoomAtPositionRaycasts(pos, true) == room)
				{
					return true;
				}
				if (extension == 0f)
				{
					return false;
				}
				pos += (room.transform.position - pos).normalized * extension;
				if (RoomUtils.RoomAtPositionRaycasts(pos, true) == room)
				{
					return true;
				}
			}
			if (extension != 0f)
			{
				pos += (room.transform.position - pos).normalized * extension;
			}
			return RoomUtils.RoomAtPosition(pos) == room;
		}

		public static RoomIdentifier RoomAtPosition(Vector3 position)
		{
			RoomIdentifier roomIdentifier;
			if (RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(position), out roomIdentifier))
			{
				return roomIdentifier;
			}
			return null;
		}

		public static RoomIdentifier RoomAtPositionRaycasts(Vector3 position, bool prioritizeRaycast = true)
		{
			RoomIdentifier roomIdentifier;
			if (!prioritizeRaycast && RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(position), out roomIdentifier) && roomIdentifier != null)
			{
				return roomIdentifier;
			}
			if (RoomUtils.TryCastRayToFindRoom(new Ray(position, Vector3.up), 8f, out roomIdentifier) || RoomUtils.TryCastRayToFindRoom(new Ray(position, Vector3.down), 8f, out roomIdentifier))
			{
				return roomIdentifier;
			}
			if (prioritizeRaycast)
			{
				return RoomUtils.RoomAtPosition(position);
			}
			return null;
		}

		public static bool TryGetRoom(Vector3 position, out RoomIdentifier room)
		{
			room = RoomUtils.RoomAtPosition(position);
			if (room == null)
			{
				room = RoomUtils.RoomAtPositionRaycasts(position, true);
			}
			return room != null;
		}

		private static bool TryCastRayToFindRoom(Ray ray, float distance, out RoomIdentifier room)
		{
			room = null;
			bool flag;
			try
			{
				RaycastHit raycastHit;
				if (Physics.Raycast(ray, out raycastHit, distance, 1))
				{
					room = raycastHit.collider.GetComponentInParent<RoomIdentifier>();
				}
				flag = room != null;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
	}
}
