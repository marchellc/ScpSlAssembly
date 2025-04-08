using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Usables.Scp1344
{
	public static class Scp1344NetworkHandler
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				Scp1344NetworkHandler.AlreadySyncedJoinNetIds.Clear();
				Scp1344NetworkHandler.ReceivedStatuses.Clear();
				NetworkClient.ReplaceHandler<Scp1344StatusMessage>(new Action<Scp1344StatusMessage>(Scp1344NetworkHandler.ClientProcessStatusMessage), true);
				NetworkServer.ReplaceHandler<Scp1344StatusMessage>(new Action<NetworkConnectionToClient, Scp1344StatusMessage>(Scp1344NetworkHandler.ServerProcessStatusMessage), true);
				NetworkClient.ReplaceHandler<Scp1344DetectionMessage>(new Action<Scp1344DetectionMessage>(Scp1344NetworkHandler.ClientProcessDetectionMessage), true);
				NetworkServer.ReplaceHandler<Scp1344DetectionMessage>(new Action<NetworkConnectionToClient, Scp1344DetectionMessage>(Scp1344NetworkHandler.ServerProcessDetectionMessage), true);
			};
			Inventory.OnLocalClientStarted += delegate
			{
				NetworkClient.Send<Scp1344StatusMessage>(new Scp1344StatusMessage(0, Scp1344Status.Idle), 0);
			};
		}

		public static void ServerSendMessage(Scp1344StatusMessage msg)
		{
			msg.SendToAuthenticated(0);
			if (NetworkClient.activeHost)
			{
				return;
			}
			Scp1344NetworkHandler.ClientProcessStatusMessage(msg);
		}

		public static Scp1344Status GetSavedStatus(ushort serial)
		{
			return Scp1344NetworkHandler.ReceivedStatuses.GetValueOrDefault(serial, Scp1344Status.Idle);
		}

		private static void ClientProcessStatusMessage(Scp1344StatusMessage msg)
		{
			Action<ushort, Scp1344Status> onStatusChanged = Scp1344NetworkHandler.OnStatusChanged;
			if (onStatusChanged != null)
			{
				onStatusChanged(msg.Serial, msg.NewState);
			}
			if (msg.NewState != Scp1344Status.Inspecting)
			{
				Scp1344NetworkHandler.ReceivedStatuses[msg.Serial] = msg.NewState;
				return;
			}
			Action<ushort, Scp1344Status> onStatusChanged2 = Scp1344NetworkHandler.OnStatusChanged;
			if (onStatusChanged2 == null)
			{
				return;
			}
			onStatusChanged2(msg.Serial, Scp1344Status.Idle);
		}

		private static void ClientProcessDetectionMessage(Scp1344DetectionMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(msg.DetectedNetId, out referenceHub))
			{
				return;
			}
			ReferenceHub referenceHub2;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub2))
			{
				return;
			}
			Action<ReferenceHub, ReferenceHub> onPlayerDetected = Scp1344NetworkHandler.OnPlayerDetected;
			if (onPlayerDetected != null)
			{
				onPlayerDetected(null, referenceHub);
			}
			referenceHub2.playerEffectsController.GetEffect<Scp1344>().PlayDetectionSound();
		}

		private static void ServerProcessStatusMessage(NetworkConnection conn, Scp1344StatusMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			Scp1344Status newState = msg.NewState;
			if (newState != Scp1344Status.Idle)
			{
				if (newState != Scp1344Status.Inspecting)
				{
					return;
				}
				Scp1344NetworkHandler.TryInspect(referenceHub);
				return;
			}
			else
			{
				if (msg.Serial != 0)
				{
					return;
				}
				if (!Scp1344NetworkHandler.AlreadySyncedJoinNetIds.Add(referenceHub.netId))
				{
					return;
				}
				Scp1344NetworkHandler.SyncAllInstances(conn);
				return;
			}
		}

		private static void ServerProcessDetectionMessage(NetworkConnection conn, Scp1344DetectionMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			if (!referenceHub.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value.ItemTypeId == ItemType.SCP1344))
			{
				return;
			}
			ReferenceHub referenceHub2;
			if (!ReferenceHub.TryGetHubNetID(msg.DetectedNetId, out referenceHub2))
			{
				return;
			}
			Action<ReferenceHub, ReferenceHub> onPlayerDetected = Scp1344NetworkHandler.OnPlayerDetected;
			if (onPlayerDetected != null)
			{
				onPlayerDetected(referenceHub, referenceHub2);
			}
			msg.SendToSpectatorsOf(referenceHub2, true);
		}

		private static void TryInspect(ReferenceHub hub)
		{
			Scp1344Item scp1344Item = hub.inventory.CurInstance as Scp1344Item;
			if (scp1344Item == null || scp1344Item == null)
			{
				return;
			}
			if (!scp1344Item.AllowInspect)
			{
				return;
			}
			Scp1344NetworkHandler.ServerSendMessage(new Scp1344StatusMessage(scp1344Item.ItemSerial, Scp1344Status.Inspecting));
		}

		private static void SyncAllInstances(NetworkConnection conn)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp1344Item scp1344Item = referenceHub.inventory.CurInstance as Scp1344Item;
				if (scp1344Item != null && !(scp1344Item == null) && scp1344Item.Status != Scp1344Status.Idle)
				{
					conn.Send<Scp1344StatusMessage>(new Scp1344StatusMessage(scp1344Item.ItemSerial, scp1344Item.Status), 0);
				}
			}
		}

		private static readonly HashSet<uint> AlreadySyncedJoinNetIds = new HashSet<uint>();

		private static readonly Dictionary<ushort, Scp1344Status> ReceivedStatuses = new Dictionary<ushort, Scp1344Status>();

		public static Action<ushort, Scp1344Status> OnStatusChanged;

		public static Action<ReferenceHub, ReferenceHub> OnPlayerDetected;
	}
}
