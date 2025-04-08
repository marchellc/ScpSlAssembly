using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ToggleableLights
{
	public static class FlashlightNetworkHandler
	{
		public static event Action<FlashlightNetworkHandler.FlashlightMessage> OnStatusReceived;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += FlashlightNetworkHandler.RegisterHandlers;
			Inventory.OnLocalClientStarted += delegate
			{
				NetworkClient.Send<FlashlightNetworkHandler.FlashlightMessage>(new FlashlightNetworkHandler.FlashlightMessage(0, false), 0);
			};
		}

		private static void RegisterHandlers()
		{
			FlashlightNetworkHandler.AlreadyRequestedFirstimeSync.Clear();
			FlashlightNetworkHandler.ReceivedStatuses.Clear();
			NetworkClient.ReplaceHandler<FlashlightNetworkHandler.FlashlightMessage>(new Action<FlashlightNetworkHandler.FlashlightMessage>(FlashlightNetworkHandler.ClientProcessMessage), true);
			NetworkServer.ReplaceHandler<FlashlightNetworkHandler.FlashlightMessage>(new Action<NetworkConnectionToClient, FlashlightNetworkHandler.FlashlightMessage>(FlashlightNetworkHandler.ServerProcessMessage), true);
		}

		private static void ClientProcessMessage(FlashlightNetworkHandler.FlashlightMessage msg)
		{
			FlashlightNetworkHandler.ReceivedStatuses[msg.Serial] = msg.NewState;
			Action<FlashlightNetworkHandler.FlashlightMessage> onStatusReceived = FlashlightNetworkHandler.OnStatusReceived;
			if (onStatusReceived != null)
			{
				onStatusReceived(msg);
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			ItemBase itemBase;
			if (!referenceHub.inventory.UserInventory.Items.TryGetValue(msg.Serial, out itemBase))
			{
				return;
			}
			ToggleableLightItemBase toggleableLightItemBase = itemBase as ToggleableLightItemBase;
			if (toggleableLightItemBase == null)
			{
				return;
			}
			toggleableLightItemBase.IsEmittingLight = msg.NewState;
		}

		private static void ServerProcessMessage(NetworkConnection conn, FlashlightNetworkHandler.FlashlightMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			if (msg.Serial == 0)
			{
				FlashlightNetworkHandler.ServerProcessFirstTimeRequest(conn);
			}
			if (referenceHub.inventory.CurItem.SerialNumber == msg.Serial)
			{
				ToggleableLightItemBase toggleableLightItemBase = referenceHub.inventory.CurInstance as ToggleableLightItemBase;
				if (toggleableLightItemBase != null)
				{
					bool flag = msg.NewState;
					PlayerTogglingFlashlightEventArgs playerTogglingFlashlightEventArgs = new PlayerTogglingFlashlightEventArgs(referenceHub, toggleableLightItemBase, flag);
					PlayerEvents.OnTogglingFlashlight(playerTogglingFlashlightEventArgs);
					if (!playerTogglingFlashlightEventArgs.IsAllowed)
					{
						return;
					}
					flag = playerTogglingFlashlightEventArgs.NewState;
					toggleableLightItemBase.IsEmittingLight = flag;
					new FlashlightNetworkHandler.FlashlightMessage(msg.Serial, flag).SendToAuthenticated(0);
					PlayerEvents.OnToggledFlashlight(new PlayerToggledFlashlightEventArgs(referenceHub, toggleableLightItemBase, flag));
					return;
				}
			}
		}

		private static void ServerProcessFirstTimeRequest(NetworkConnection conn)
		{
			if (!FlashlightNetworkHandler.AlreadyRequestedFirstimeSync.Add(conn.identity.netId))
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				ToggleableLightItemBase toggleableLightItemBase = referenceHub.inventory.CurInstance as ToggleableLightItemBase;
				if (toggleableLightItemBase != null && !(toggleableLightItemBase == null) && !toggleableLightItemBase.IsEmittingLight)
				{
					conn.Send<FlashlightNetworkHandler.FlashlightMessage>(new FlashlightNetworkHandler.FlashlightMessage(referenceHub.inventory.CurItem.SerialNumber, false), 0);
				}
			}
		}

		public static void Serialize(this NetworkWriter writer, FlashlightNetworkHandler.FlashlightMessage value)
		{
			writer.WriteUShort(value.Serial);
			writer.WriteBool(value.NewState);
		}

		public static FlashlightNetworkHandler.FlashlightMessage Deserialize(this NetworkReader reader)
		{
			return new FlashlightNetworkHandler.FlashlightMessage(reader.ReadUShort(), reader.ReadBool());
		}

		private static readonly HashSet<uint> AlreadyRequestedFirstimeSync = new HashSet<uint>();

		public static readonly Dictionary<ushort, bool> ReceivedStatuses = new Dictionary<ushort, bool>();

		public readonly struct FlashlightMessage : NetworkMessage
		{
			public FlashlightMessage(ushort flashlightSerial, bool newState)
			{
				this.Serial = flashlightSerial;
				this.NewState = newState;
			}

			public readonly ushort Serial;

			public readonly bool NewState;
		}
	}
}
