using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public static class RoleSpawnpointManager
	{
		public static bool TryGetSpawnpointForRole(RoleTypeId role, out ISpawnpointHandler spawnpoint)
		{
			bool flag = false;
			List<ISpawnpointHandler> list = new List<ISpawnpointHandler>();
			foreach (RoleSpawnpointManager.SpawnpointDefinition spawnpointDefinition in RoleSpawnpointManager.DefinedSpawnpoints)
			{
				if (spawnpointDefinition.Roles.Contains(role))
				{
					flag = true;
					list.AddRange(spawnpointDefinition.CompatibleSpawnpoints);
				}
			}
			spawnpoint = (flag ? list.RandomItem<ISpawnpointHandler>() : null);
			return flag;
		}

		internal static void SetPosition(ReferenceHub hub, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			IFpcRole fpcRole = newRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			if (fpcRole.SpawnpointHandler == null)
			{
				return;
			}
			Vector3 spawnLocation;
			float horizontalRotation;
			bool flag = fpcRole.SpawnpointHandler.TryGetSpawnpoint(out spawnLocation, out horizontalRotation);
			PlayerSpawningEventArgs playerSpawningEventArgs = new PlayerSpawningEventArgs(hub, newRole, flag, spawnLocation, horizontalRotation);
			PlayerEvents.OnSpawning(playerSpawningEventArgs);
			if (!playerSpawningEventArgs.IsAllowed)
			{
				return;
			}
			spawnLocation = playerSpawningEventArgs.SpawnLocation;
			horizontalRotation = playerSpawningEventArgs.HorizontalRotation;
			flag = playerSpawningEventArgs.UseSpawnPoint;
			if (!newRole.ServerSpawnFlags.HasFlag(RoleSpawnFlags.UseSpawnpoint) || !flag)
			{
				PlayerEvents.OnSpawned(new PlayerSpawnedEventArgs(hub, newRole, flag, spawnLocation, horizontalRotation));
				return;
			}
			hub.transform.position = spawnLocation;
			if (fpcRole.FpcModule.MouseLook != null)
			{
				fpcRole.FpcModule.MouseLook.CurrentHorizontal = horizontalRotation;
			}
			PlayerEvents.OnSpawned(new PlayerSpawnedEventArgs(hub, newRole, flag, spawnLocation, horizontalRotation));
		}

		private static readonly RoleSpawnpointManager.SpawnpointDefinition[] DefinedSpawnpoints = new RoleSpawnpointManager.SpawnpointDefinition[] { new RoleSpawnpointManager.SpawnpointDefinition(new RoleTypeId[] { RoleTypeId.ClassD }).SetSpawnpoints(new ISpawnpointHandler[]
		{
			new RoomRoleSpawnpoint(new Vector3(-6.18f, 0.91f, -4.23f), 5f, 0f, 26.26f, 0.73f, 7, 1, RoomName.LczClassDSpawn, FacilityZone.None, RoomShape.Undefined),
			new RoomRoleSpawnpoint(new Vector3(-6.18f, 0.91f, 4.23f), 175f, 0f, 26.26f, 0.73f, 7, 1, RoomName.LczClassDSpawn, FacilityZone.None, RoomShape.Undefined)
		}) };

		private struct SpawnpointDefinition
		{
			public SpawnpointDefinition(params RoleTypeId[] roles)
			{
				this.Roles = roles;
				this.CompatibleSpawnpoints = null;
			}

			public RoleSpawnpointManager.SpawnpointDefinition SetSpawnpoints(params ISpawnpointHandler[] spawnpoints)
			{
				this.CompatibleSpawnpoints = spawnpoints;
				return this;
			}

			public RoleTypeId[] Roles;

			public ISpawnpointHandler[] CompatibleSpawnpoints;
		}
	}
}
