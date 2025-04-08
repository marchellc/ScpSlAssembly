using System;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Keycards
{
	public class KeycardItem : ItemBase, IItemNametag
	{
		public static event Action<ushort> OnKeycardUsed;

		public override float Weight
		{
			get
			{
				return 0.01f;
			}
		}

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Keycard;
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += KeycardItem.OnClientReady;
		}

		private static void OnClientReady()
		{
			NetworkServer.ReplaceHandler<KeycardItem.UseMessage>(new Action<NetworkConnectionToClient, KeycardItem.UseMessage>(KeycardItem.ServerProcessMessage), true);
			NetworkClient.ReplaceHandler<KeycardItem.UseMessage>(new Action<KeycardItem.UseMessage>(KeycardItem.ClientProcessMessage), true);
		}

		private static void ServerProcessMessage(NetworkConnection conn, KeycardItem.UseMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			KeycardItem keycardItem = referenceHub.inventory.CurInstance as KeycardItem;
			if (keycardItem == null || keycardItem == null)
			{
				return;
			}
			if (msg.ItemSerial != keycardItem.ItemSerial)
			{
				return;
			}
			msg.SendToAuthenticated(0);
		}

		private static void ClientProcessMessage(KeycardItem.UseMessage msg)
		{
			Action<ushort> onKeycardUsed = KeycardItem.OnKeycardUsed;
			if (onKeycardUsed == null)
			{
				return;
			}
			onKeycardUsed(msg.ItemSerial);
		}

		public KeycardPermissions Permissions;

		public struct UseMessage : NetworkMessage
		{
			public UseMessage(ushort serial)
			{
				this.ItemSerial = serial;
			}

			public ushort ItemSerial;
		}
	}
}
