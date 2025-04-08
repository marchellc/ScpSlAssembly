using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class ElevatorOverconRenderer : PooledOverconRenderer
	{
		internal override void SpawnOvercons(Scp079Camera newCamera)
		{
			base.ReturnAll();
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(newCamera.Room, out hashSet))
			{
				return;
			}
			foreach (DoorVariant doorVariant in hashSet)
			{
				ElevatorDoor elevatorDoor = doorVariant as ElevatorDoor;
				if (elevatorDoor != null)
				{
					ElevatorOvercon fromPool = base.GetFromPool<ElevatorOvercon>();
					fromPool.Target = elevatorDoor;
					fromPool.transform.position = elevatorDoor.transform.position + Vector3.up * 1.25f;
					fromPool.Rescale(newCamera);
				}
			}
		}

		private const float Height = 1.25f;
	}
}
