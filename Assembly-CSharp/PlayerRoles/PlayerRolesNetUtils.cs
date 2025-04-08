using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace PlayerRoles
{
	public static class PlayerRolesNetUtils
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				PlayerRolesNetUtils.QueuedRoles.Clear();
				PlayerRolesNetUtils.InfoPackAlreadyReceived = NetworkServer.active;
				NetworkClient.ReplaceHandler<RoleSyncInfo>(delegate(RoleSyncInfo rsi)
				{
				}, true);
				NetworkClient.ReplaceHandler<RoleSyncInfoPack>(delegate(RoleSyncInfoPack rsip)
				{
					PlayerRolesNetUtils.InfoPackAlreadyReceived = true;
				}, true);
			};
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(PlayerRolesNetUtils.HandleSpawnedPlayer));
		}

		private static void HandleSpawnedPlayer(ReferenceHub hub)
		{
			if (NetworkServer.active)
			{
				if (!hub.isLocalPlayer)
				{
					hub.connectionToClient.Send<RoleSyncInfoPack>(new RoleSyncInfoPack(hub), 0);
				}
				return;
			}
			NetworkReader networkReader;
			if (!PlayerRolesNetUtils.QueuedRoles.TryGetValue(hub.netId, out networkReader))
			{
				return;
			}
			hub.roleManager.InitializeNewRole(networkReader.ReadRoleType(), RoleChangeReason.None, RoleSpawnFlags.All, networkReader);
		}

		public static void WriteRoleSyncInfo(this NetworkWriter writer, RoleSyncInfo info)
		{
			info.Write(writer);
		}

		public static RoleSyncInfo ReadRoleSyncInfo(this NetworkReader reader)
		{
			return new RoleSyncInfo(reader);
		}

		public static void WriteRoleSyncInfoPack(this NetworkWriter writer, RoleSyncInfoPack info)
		{
			info.WritePlayers(writer);
		}

		public static RoleSyncInfoPack ReadRoleSyncInfoPack(this NetworkReader reader)
		{
			return new RoleSyncInfoPack(reader);
		}

		public static readonly Dictionary<uint, NetworkReader> QueuedRoles = new Dictionary<uint, NetworkReader>();

		public static bool InfoPackAlreadyReceived;
	}
}
