using System.Collections.Generic;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints;

public class RoundConsistentSpawnpointHandler : StandardSpawnpointHandler
{
	private static readonly Dictionary<RoleTypeId, CachedRoom> CachedRoleSpawnpoints = new Dictionary<RoleTypeId, CachedRoom>();

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationFinished += HandleMapGenerationFinished;
	}

	private static void HandleMapGenerationFinished()
	{
		if (NetworkServer.active)
		{
			RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.Clear();
		}
	}

	public override bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
	{
		position = default(Vector3);
		horizontalRot = 0f;
		RoomRoleSpawnpoint[] validSpawnpoints = base.GetValidSpawnpoints(base.Spawnpoints);
		if (validSpawnpoints.Length == 0)
		{
			return false;
		}
		if (!RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.TryGetValue(base.Role.RoleTypeId, out var value))
		{
			RoomRoleSpawnpoint roomRoleSpawnpoint = validSpawnpoints.RandomItem();
			int roomIndex = Random.Range(0, roomRoleSpawnpoint.GetRoomAmount() - 1);
			value = new CachedRoom(roomRoleSpawnpoint, roomIndex);
			RoundConsistentSpawnpointHandler.CachedRoleSpawnpoints.Add(base.Role.RoleTypeId, value);
		}
		return value.RoomType.TryGetSpawnpoint(out position, out horizontalRot, value.RoomIndex);
	}
}
