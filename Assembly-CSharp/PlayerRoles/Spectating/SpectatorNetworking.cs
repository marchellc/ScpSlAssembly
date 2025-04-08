using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.Spectating
{
	public static class SpectatorNetworking
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<SpectatorNetworking.SpectatedNetIdSyncMessage>(delegate(NetworkConnectionToClient conn, SpectatorNetworking.SpectatedNetIdSyncMessage msg)
				{
					ReferenceHub referenceHub;
					if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
					{
						return;
					}
					SpectatorRole spectatorRole = referenceHub.roleManager.CurrentRole as SpectatorRole;
					if (spectatorRole == null)
					{
						return;
					}
					ReferenceHub referenceHub2;
					ReferenceHub.TryGetHubNetID(spectatorRole.SyncedSpectatedNetId, out referenceHub2);
					ReferenceHub referenceHub3;
					ReferenceHub.TryGetHubNetID(msg.NetId, out referenceHub3);
					spectatorRole.SyncedSpectatedNetId = msg.NetId;
					PlayerEvents.OnChangedSpectator(new PlayerChangedSpectatorEventArgs(referenceHub, referenceHub2, referenceHub3));
				}, true);
			};
			SpectatorTargetTracker.OnTargetChanged += delegate
			{
				ReferenceHub referenceHub4;
				NetworkClient.Send<SpectatorNetworking.SpectatedNetIdSyncMessage>(new SpectatorNetworking.SpectatedNetIdSyncMessage
				{
					NetId = (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub4) ? referenceHub4.netId : 0U)
				}, 0);
			};
		}

		public static void SendToSpectatorsOf<T>(this T msg, ReferenceHub target, bool includeTarget = false) where T : struct, NetworkMessage
		{
			msg.SendToHubsConditionally((ReferenceHub x) => target.IsSpectatedBy(x) || (includeTarget && x == target), 0);
		}

		public static void ForeachSpectatorOf(ReferenceHub target, Action<ReferenceHub> action)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (target.IsSpectatedBy(referenceHub))
				{
					action(referenceHub);
				}
			}
		}

		public static bool IsSpectatedBy(this ReferenceHub target, ReferenceHub spectator)
		{
			SpectatorRole spectatorRole = spectator.roleManager.CurrentRole as SpectatorRole;
			return spectatorRole != null && spectatorRole.SyncedSpectatedNetId == target.netId;
		}

		public static bool IsLocallySpectated(this ReferenceHub target)
		{
			ReferenceHub referenceHub;
			return SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub) && referenceHub == target;
		}

		public struct SpectatedNetIdSyncMessage : NetworkMessage
		{
			public uint NetId;
		}
	}
}
