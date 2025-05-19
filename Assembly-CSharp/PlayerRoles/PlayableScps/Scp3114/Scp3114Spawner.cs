using System.Collections.Generic;
using MapGeneration;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public static class Scp3114Spawner
{
	private const float SpawnChance = 0f;

	private const int MinHumans = 2;

	private static readonly List<ReferenceHub> SpawnCandidates = new List<ReferenceHub>();

	private static bool _ragdollsSpawned;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RoleAssigner.OnPlayersSpawned += OnPlayersSpawned;
		PlayerRoleManager.OnServerRoleSet += OnServerRoleSet;
	}

	private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
	{
		if (newRole == RoleTypeId.Scp3114)
		{
			ServerSpawnRagdolls(userHub);
		}
	}

	private static void OnPlayersSpawned()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		_ragdollsSpawned = false;
		if (!(Random.value >= 0f))
		{
			SpawnCandidates.Clear();
			PlayerRolesUtils.ForEachRole<HumanRole>(SpawnCandidates.Add);
			if (SpawnCandidates.Count >= 2)
			{
				SpawnCandidates.RandomItem().roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart);
			}
		}
	}

	private static void ServerSpawnRagdolls(ReferenceHub nicknameSourceHub)
	{
		if (!_ragdollsSpawned && RoomUtils.TryFindRoom(RoomName.Lcz173, FacilityZone.LightContainment, RoomShape.Endroom, out var foundRoom))
		{
			_ragdollsSpawned = true;
			Transform transform = foundRoom.transform;
			ServerSpawnRagdoll(RoleTypeId.Scientist, transform.TransformPoint(new Vector3(2.05f, 12.5f, 1.74f)), transform.rotation * Quaternion.Euler(25.5f, -35.5f, -9.4f), nicknameSourceHub);
			ServerSpawnRagdoll(RoleTypeId.ClassD, transform.TransformPoint(new Vector3(2.9f, 12.5f, 1.74f)), transform.rotation * Quaternion.Euler(22.2f, -1.3f, 11.8f), nicknameSourceHub);
		}
	}

	private static void ServerSpawnRagdoll(RoleTypeId role, Vector3 pos, Quaternion rot, ReferenceHub nicknameSource)
	{
		if (PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(role, out var result))
		{
			BasicRagdoll basicRagdoll = result.Ragdoll.ServerInstantiateSelf(nicknameSource, role);
			basicRagdoll.NetworkInfo = new RagdollData(null, new Scp3114DamageHandler(basicRagdoll, isStarting: true), role, pos, rot, nicknameSource.nicknameSync.DisplayName, NetworkTime.time, 0);
			NetworkServer.Spawn(basicRagdoll.gameObject);
		}
	}
}
