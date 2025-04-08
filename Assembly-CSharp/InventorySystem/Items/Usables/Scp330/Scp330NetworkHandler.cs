using System;
using System.Collections.Generic;
using Footprinting;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp330
{
	public static class Scp330NetworkHandler
	{
		public static event Action<SelectScp330Message> OnClientSelectMessageReceived;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += Scp330NetworkHandler.RegisterHandlers;
		}

		private static void RegisterHandlers()
		{
			NetworkServer.ReplaceHandler<SelectScp330Message>(new Action<NetworkConnectionToClient, SelectScp330Message>(Scp330NetworkHandler.ServerSelectMessageReceived), true);
			NetworkClient.ReplaceHandler<SelectScp330Message>(new Action<SelectScp330Message>(Scp330NetworkHandler.ClientSelectMessageReceived), true);
			NetworkClient.ReplaceHandler<SyncScp330Message>(new Action<SyncScp330Message>(Scp330NetworkHandler.ClientSyncMessageReceived), true);
			Scp330NetworkHandler.ReceivedSelectedCandies.Clear();
		}

		private static void ServerSelectMessageReceived(NetworkConnection conn, SelectScp330Message msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			Scp330Bag scp330Bag = referenceHub.inventory.CurInstance as Scp330Bag;
			if (scp330Bag == null || scp330Bag == null)
			{
				return;
			}
			if (scp330Bag.ItemSerial != msg.Serial || msg.CandyID >= scp330Bag.Candies.Count)
			{
				return;
			}
			if (msg.Drop)
			{
				PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(scp330Bag.ItemTypeId, scp330Bag.Weight, 0, false);
				Scp330Pickup scp330Pickup = referenceHub.inventory.ServerCreatePickup(scp330Bag, new PickupSyncInfo?(pickupSyncInfo), true, null) as Scp330Pickup;
				if (scp330Pickup == null)
				{
					return;
				}
				scp330Pickup.PreviousOwner = new Footprint(referenceHub);
				CandyKindID candyKindID = scp330Bag.TryRemove(msg.CandyID);
				if (candyKindID == CandyKindID.None)
				{
					return;
				}
				scp330Pickup.NetworkExposedCandy = candyKindID;
				scp330Pickup.StoredCandies.Add(candyKindID);
				return;
			}
			else
			{
				if (msg.CandyID < 0 || msg.CandyID >= scp330Bag.Candies.Count)
				{
					return;
				}
				scp330Bag.SelectedCandyId = msg.CandyID;
				msg.CandyID = (int)scp330Bag.Candies[msg.CandyID];
				PlayerHandler handler = UsableItemsController.GetHandler(referenceHub);
				handler.CurrentUsable = new CurrentlyUsedItem(scp330Bag, msg.Serial, Time.timeSinceLevelLoad);
				handler.CurrentUsable.Item.OnUsingStarted();
				msg.SendToAuthenticated(0);
				return;
			}
		}

		private static void ClientSyncMessageReceived(SyncScp330Message msg)
		{
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
			Scp330Bag scp330Bag = itemBase as Scp330Bag;
			if (scp330Bag != null)
			{
				scp330Bag.Candies = msg.Candies;
			}
		}

		private static void ClientSelectMessageReceived(SelectScp330Message msg)
		{
			CandyKindID candyKindID = (CandyKindID)msg.CandyID;
			Scp330NetworkHandler.ReceivedSelectedCandies[msg.Serial] = candyKindID;
			Action<SelectScp330Message> onClientSelectMessageReceived = Scp330NetworkHandler.OnClientSelectMessageReceived;
			if (onClientSelectMessageReceived != null)
			{
				onClientSelectMessageReceived(msg);
			}
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(msg.Serial, out referenceHub))
			{
				return;
			}
			if (referenceHub.isLocalPlayer)
			{
				Scp330Bag scp330Bag = referenceHub.inventory.CurInstance as Scp330Bag;
				if (scp330Bag != null && scp330Bag != null)
				{
					scp330Bag.OnUsingStarted();
				}
			}
			Scp330Bag scp330Bag2;
			if (!InventoryItemLoader.TryGetItem<Scp330Bag>(ItemType.SCP330, out scp330Bag2))
			{
				return;
			}
			UsableItemsController.PlaySoundOnPlayer(referenceHub, Scp330Viewmodel.GetClipForCandy(candyKindID));
			UsableItemsController.StartTimes[msg.Serial] = Time.timeSinceLevelLoad;
		}

		public static void SerializeSyncMessage(this NetworkWriter writer, SyncScp330Message value)
		{
			writer.WriteUShort(value.Serial);
			writer.WriteByte((byte)value.Candies.Count);
			foreach (CandyKindID candyKindID in value.Candies)
			{
				writer.WriteByte((byte)candyKindID);
			}
		}

		public static SyncScp330Message DeserializeSyncMessage(this NetworkReader reader)
		{
			ushort num = reader.ReadUShort();
			byte b = reader.ReadByte();
			List<CandyKindID> list = new List<CandyKindID>();
			for (int i = 0; i < (int)b; i++)
			{
				list.Add((CandyKindID)reader.ReadByte());
			}
			return new SyncScp330Message
			{
				Candies = list,
				Serial = num
			};
		}

		public static void SerializeSelectMessage(this NetworkWriter writer, SelectScp330Message value)
		{
			int num = value.CandyID + 1;
			writer.WriteUShort(value.Serial);
			writer.WriteSByte((sbyte)(value.Drop ? (-(sbyte)num) : num));
		}

		public static SelectScp330Message DeserializeSelectMessage(this NetworkReader reader)
		{
			ushort num = reader.ReadUShort();
			int num2 = (int)reader.ReadSByte();
			return new SelectScp330Message
			{
				CandyID = Mathf.Abs(num2) - 1,
				Serial = num,
				Drop = (num2 < 0)
			};
		}

		public static readonly Dictionary<ushort, CandyKindID> ReceivedSelectedCandies = new Dictionary<ushort, CandyKindID>();
	}
}
