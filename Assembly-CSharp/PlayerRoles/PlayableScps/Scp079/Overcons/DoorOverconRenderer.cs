using System;
using System.Collections.Generic;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class DoorOverconRenderer : PooledOverconRenderer
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
				if (Scp079DoorAbility.CheckVisibility(doorVariant, newCamera))
				{
					CheckpointDoor checkpointDoor = doorVariant as CheckpointDoor;
					Vector3 vector2;
					if (checkpointDoor != null)
					{
						Vector3 vector = Vector3.zero;
						foreach (DoorVariant doorVariant2 in checkpointDoor.SubDoors)
						{
							vector += doorVariant2.transform.position;
						}
						vector2 = vector / (float)checkpointDoor.SubDoors.Length;
					}
					else
					{
						vector2 = doorVariant.transform.position;
					}
					DoorOvercon fromPool = base.GetFromPool<DoorOvercon>();
					fromPool.Target = doorVariant;
					fromPool.transform.position = vector2 + Vector3.up * 1.25f;
					fromPool.Rescale(newCamera);
				}
			}
		}

		private const float DoorHeight = 1.25f;
	}
}
