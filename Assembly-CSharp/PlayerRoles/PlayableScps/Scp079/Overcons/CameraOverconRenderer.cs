using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class CameraOverconRenderer : PooledOverconRenderer
	{
		internal override void SpawnOvercons(Scp079Camera newCamera)
		{
			base.ReturnAll();
			CameraOverconRenderer.VisibleOvercons.Clear();
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				if (this.CheckVisibility(newCamera, scp079InteractableBase))
				{
					CameraOvercon fromPool = base.GetFromPool<CameraOvercon>();
					fromPool.Setup(newCamera, scp079InteractableBase as Scp079Camera, false);
					CameraOverconRenderer.VisibleOvercons.Add(fromPool);
				}
			}
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(newCamera.Room, out hashSet))
			{
				return;
			}
			foreach (DoorVariant doorVariant in hashSet)
			{
				ElevatorDoor elevatorDoor = doorVariant as ElevatorDoor;
				Scp079Camera scp079Camera;
				if (elevatorDoor != null && Mathf.Abs(elevatorDoor.TargetPosition.y - newCamera.Position.y) <= 40f && Scp079Camera.TryGetClosestCamera(elevatorDoor.transform.position, null, out scp079Camera) && !(scp079Camera != newCamera))
				{
					List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(elevatorDoor.Group);
					ElevatorDoor targetElevator = doorsForGroup[(doorsForGroup.IndexOf(elevatorDoor) + 1) % doorsForGroup.Count];
					Scp079Camera scp079Camera2;
					if (targetElevator.Rooms.Length == 1 && Scp079Camera.TryGetClosestCamera(targetElevator.transform.position, (Scp079Camera x) => x.Room == targetElevator.Rooms[0], out scp079Camera2))
					{
						CameraOvercon fromPool2 = base.GetFromPool<CameraOvercon>();
						fromPool2.Setup(newCamera, scp079Camera2, true);
						fromPool2.Position = elevatorDoor.transform.position + Vector3.up * 3f;
						fromPool2.Rescale(newCamera);
						CameraOverconRenderer.VisibleOvercons.Add(fromPool2);
					}
				}
			}
		}

		private bool CheckVisibility(Scp079Camera cur, Scp079InteractableBase target)
		{
			Scp079Camera scp079Camera = target as Scp079Camera;
			if (scp079Camera == null || cur == scp079Camera)
			{
				return false;
			}
			Vector3 vector = cur.Position - target.Position;
			return vector.MagnitudeOnlyY() <= 40f && (cur.Room == target.Room || (vector.sqrMagnitude <= 2165f && scp079Camera.IsMain && cur.Room.ConnectedRooms.Contains(scp079Camera.Room)));
		}

		private const float MaxSqrDistanceOtherRoom = 2165f;

		private const float MaxHeightDiff = 40f;

		private const float ElevatorIconHeight = 3f;

		public static HashSet<CameraOvercon> VisibleOvercons = new HashSet<CameraOvercon>();
	}
}
