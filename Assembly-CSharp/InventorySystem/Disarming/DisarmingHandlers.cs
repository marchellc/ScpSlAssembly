using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Disarming
{
	public static class DisarmingHandlers
	{
		public static event DisarmingHandlers.PlayerDisarmed OnPlayerDisarmed;

		private static DisarmedPlayersListMessage NewDisarmedList
		{
			get
			{
				return new DisarmedPlayersListMessage(DisarmedPlayers.Entries);
			}
		}

		public static void InvokeOnPlayerDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			DisarmingHandlers.PlayerDisarmed onPlayerDisarmed = DisarmingHandlers.OnPlayerDisarmed;
			if (onPlayerDisarmed == null)
			{
				return;
			}
			onPlayerDisarmed(disarmerHub, targetHub);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += DisarmingHandlers.ReplaceHandlers;
			Inventory.OnLocalClientStarted += delegate
			{
				NetworkClient.Send<DisarmMessage>(new DisarmMessage(null, false, true), 0);
			};
		}

		private static void ReplaceHandlers()
		{
			DisarmedPlayers.Entries.Clear();
			DisarmingHandlers.ServerCooldowns.Clear();
			NetworkServer.ReplaceHandler<DisarmMessage>(new Action<NetworkConnectionToClient, DisarmMessage>(DisarmingHandlers.ServerProcessDisarmMessage), true);
			NetworkClient.ReplaceHandler<DisarmedPlayersListMessage>(new Action<DisarmedPlayersListMessage>(DisarmingHandlers.ClientProcessListMessage), true);
		}

		private static void ServerProcessDisarmMessage(NetworkConnection conn, DisarmMessage msg)
		{
			if (!NetworkServer.active || !DisarmingHandlers.ServerCheckCooldown(conn))
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			if (!msg.PlayerIsNull)
			{
				if ((msg.PlayerToDisarm.transform.position - referenceHub.transform.position).sqrMagnitude > 20f)
				{
					return;
				}
				if (msg.PlayerToDisarm.inventory.CurInstance != null && msg.PlayerToDisarm.inventory.CurInstance.TierFlags != ItemTierFlags.Common)
				{
					return;
				}
			}
			bool flag = !msg.PlayerIsNull && msg.PlayerToDisarm.inventory.IsDisarmed();
			bool flag2 = !msg.PlayerIsNull && referenceHub.CanStartDisarming(msg.PlayerToDisarm);
			if (flag && !msg.Disarm)
			{
				if (!referenceHub.inventory.IsDisarmed())
				{
					bool flag3 = referenceHub.GetTeam() == Team.SCPs;
					PlayerUncuffingEventArgs playerUncuffingEventArgs = new PlayerUncuffingEventArgs(referenceHub, msg.PlayerToDisarm, !flag3);
					PlayerEvents.OnUncuffing(playerUncuffingEventArgs);
					if (!playerUncuffingEventArgs.IsAllowed)
					{
						return;
					}
					if (flag3)
					{
						return;
					}
					msg.PlayerToDisarm.inventory.SetDisarmedStatus(null);
					PlayerEvents.OnUncuffed(new PlayerUncuffedEventArgs(referenceHub, msg.PlayerToDisarm, !flag3));
				}
			}
			else
			{
				if (flag || !flag2 || !msg.Disarm)
				{
					referenceHub.networkIdentity.connectionToClient.Send<DisarmedPlayersListMessage>(DisarmingHandlers.NewDisarmedList, 0);
					return;
				}
				if (msg.PlayerToDisarm.inventory.CurInstance == null || msg.PlayerToDisarm.inventory.CurInstance.AllowHolster)
				{
					PlayerCuffingEventArgs playerCuffingEventArgs = new PlayerCuffingEventArgs(referenceHub, msg.PlayerToDisarm);
					PlayerEvents.OnCuffing(playerCuffingEventArgs);
					if (!playerCuffingEventArgs.IsAllowed)
					{
						return;
					}
					DisarmingHandlers.InvokeOnPlayerDisarmed(referenceHub, msg.PlayerToDisarm);
					msg.PlayerToDisarm.inventory.SetDisarmedStatus(referenceHub.inventory);
					PlayerEvents.OnCuffed(new PlayerCuffedEventArgs(referenceHub, msg.PlayerToDisarm));
				}
			}
			DisarmingHandlers.NewDisarmedList.SendToAuthenticated(0);
		}

		private static bool ServerCheckCooldown(NetworkConnection conn)
		{
			uint netId = conn.identity.netId;
			float timeSinceLevelLoad = Time.timeSinceLevelLoad;
			float num;
			if (!DisarmingHandlers.ServerCooldowns.TryGetValue(conn.identity.netId, out num))
			{
				num = 0f;
			}
			if (timeSinceLevelLoad < num + 0.8f)
			{
				return false;
			}
			DisarmingHandlers.ServerCooldowns[netId] = timeSinceLevelLoad;
			return true;
		}

		private static void ClientProcessListMessage(DisarmedPlayersListMessage msg)
		{
			if (NetworkServer.active)
			{
				return;
			}
			DisarmedPlayers.Entries = msg.Entries;
		}

		private static readonly Dictionary<uint, float> ServerCooldowns = new Dictionary<uint, float>();

		private const float ServerDisarmingDistanceSqrt = 20f;

		private const float ServerRequestCooldown = 0.8f;

		public delegate void PlayerDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub);
	}
}
