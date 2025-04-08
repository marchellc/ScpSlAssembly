using System;
using System.Collections.Generic;
using MapGeneration;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public static class Scp3114Spawner
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			RoleAssigner.OnPlayersSpawned += Scp3114Spawner.OnPlayersSpawned;
			PlayerRoleManager.OnServerRoleSet += Scp3114Spawner.OnServerRoleSet;
		}

		private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (newRole != RoleTypeId.Scp3114)
			{
				return;
			}
			Scp3114Spawner.ServerSpawnRagdolls(userHub);
		}

		private static void OnPlayersSpawned()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp3114Spawner._ragdollsSpawned = false;
			if (global::UnityEngine.Random.value >= 0f)
			{
				return;
			}
			Scp3114Spawner.SpawnCandidates.Clear();
			PlayerRolesUtils.ForEachRole<HumanRole>(new Action<ReferenceHub>(Scp3114Spawner.SpawnCandidates.Add));
			if (Scp3114Spawner.SpawnCandidates.Count < 2)
			{
				return;
			}
			Scp3114Spawner.SpawnCandidates.RandomItem<ReferenceHub>().roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
		}

		private static void ServerSpawnRagdolls(ReferenceHub nicknameSourceHub)
		{
			if (Scp3114Spawner._ragdollsSpawned)
			{
				return;
			}
			RoomIdentifier roomIdentifier;
			if (!RoomUtils.TryFindRoom(RoomName.Lcz173, FacilityZone.LightContainment, RoomShape.Endroom, out roomIdentifier))
			{
				return;
			}
			Scp3114Spawner._ragdollsSpawned = true;
			Transform transform = roomIdentifier.transform;
			Scp3114Spawner.ServerSpawnRagdoll(RoleTypeId.Scientist, transform.TransformPoint(new Vector3(2.2f, 12f, 1.1f)), transform.rotation * Quaternion.Euler(-30f, 170f, 0f), nicknameSourceHub);
			Scp3114Spawner.ServerSpawnRagdoll(RoleTypeId.ClassD, transform.TransformPoint(new Vector3(3f, 12f, 1f)), transform.rotation * Quaternion.Euler(-30f, 190.7f, 7.5f), nicknameSourceHub);
		}

		private static void ServerSpawnRagdoll(RoleTypeId role, Vector3 pos, Quaternion rot, ReferenceHub nicknameSource)
		{
			HumanRole humanRole;
			if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(role, out humanRole))
			{
				return;
			}
			BasicRagdoll basicRagdoll = humanRole.Ragdoll.ServerInstantiateSelf(nicknameSource, role);
			basicRagdoll.NetworkInfo = new RagdollData(null, new Scp3114DamageHandler(basicRagdoll, true), role, pos, rot, nicknameSource.nicknameSync.DisplayName, NetworkTime.time, 0);
			NetworkServer.Spawn(basicRagdoll.gameObject, null);
		}

		private const float SpawnChance = 0f;

		private const int MinHumans = 2;

		private static readonly List<ReferenceHub> SpawnCandidates = new List<ReferenceHub>();

		private static bool _ragdollsSpawned;
	}
}
