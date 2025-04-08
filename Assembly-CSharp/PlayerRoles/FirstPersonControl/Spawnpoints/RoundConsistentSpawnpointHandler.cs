using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public class RoundConsistentSpawnpointHandler : StandardSpawnpointHandler
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationFinished += RoundConsistentSpawnpointHandler.HandleMapGenerationFinished;
		}

		private static void HandleMapGenerationFinished()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.Clear();
		}

		public override bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
		{
			position = default(Vector3);
			horizontalRot = 0f;
			RoomRoleSpawnpoint[] validSpawnpoints = base.GetValidSpawnpoints(this.Spawnpoints);
			if (validSpawnpoints.Length == 0)
			{
				return false;
			}
			CachedRoom cachedRoom;
			if (!RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.TryGetValue(base.Role.RoleTypeId, out cachedRoom))
			{
				RoomRoleSpawnpoint roomRoleSpawnpoint = validSpawnpoints.RandomItem<RoomRoleSpawnpoint>();
				int num = global::UnityEngine.Random.Range(0, roomRoleSpawnpoint.GetRoomAmount() - 1);
				cachedRoom = new CachedRoom(roomRoleSpawnpoint, num);
				RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.Add(base.Role.RoleTypeId, cachedRoom);
			}
			return cachedRoom.RoomType.TryGetSpawnpoint(out position, out horizontalRot, cachedRoom.RoomIndex);
		}

		private static readonly Dictionary<RoleTypeId, CachedRoom> CachedRoleSpawnpoints = new Dictionary<RoleTypeId, CachedRoom>();
	}
}
