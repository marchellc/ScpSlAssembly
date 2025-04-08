using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Radio
{
	public static class RadioMessages
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<RadioStatusMessage>(new Action<RadioStatusMessage>(RadioMessages.ClientStatusReceived), true);
				NetworkServer.ReplaceHandler<ClientRadioCommandMessage>(new Action<NetworkConnectionToClient, ClientRadioCommandMessage>(RadioMessages.ServerCommandReceived), true);
				RadioMessages.SyncedRangeLevels.Clear();
			};
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				foreach (KeyValuePair<uint, RadioStatusMessage> keyValuePair in RadioMessages.SyncedRangeLevels)
				{
					hub.connectionToClient.Send<RadioStatusMessage>(keyValuePair.Value, 0);
				}
			}));
		}

		private static void ServerCommandReceived(NetworkConnection conn, ClientRadioCommandMessage msg)
		{
			RadioItem radioItem;
			if (RadioMessages.GetRadio(ReferenceHub.GetHub(conn.identity.gameObject), out radioItem))
			{
				radioItem.ServerProcessCmd(msg.Command);
			}
		}

		private static void ClientStatusReceived(RadioStatusMessage msg)
		{
			RadioMessages.SyncedRangeLevels[msg.Owner] = msg;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			RadioItem radioItem;
			if (!RadioMessages.GetRadio(referenceHub, out radioItem))
			{
				return;
			}
			if (radioItem.Owner.netId != msg.Owner)
			{
				return;
			}
			radioItem.UserReceiveInfo(msg);
		}

		private static bool GetRadio(ReferenceHub ply, out RadioItem radio)
		{
			radio = null;
			if (ply == null)
			{
				return false;
			}
			foreach (ItemBase itemBase in ply.inventory.UserInventory.Items.Values)
			{
				RadioItem radioItem = itemBase as RadioItem;
				if (radioItem != null)
				{
					radio = radioItem;
					return true;
				}
			}
			return false;
		}

		public static void WriteRadioStatusMessage(this NetworkWriter writer, RadioStatusMessage msg)
		{
			msg.Serialize(writer);
		}

		public static RadioStatusMessage ReadRadioStatusMessage(this NetworkReader reader)
		{
			return new RadioStatusMessage(reader);
		}

		public static void WriteClientRadioCommandMessage(this NetworkWriter writer, ClientRadioCommandMessage msg)
		{
			msg.Serialize(writer);
		}

		public static ClientRadioCommandMessage ReadClientRadioCommandMessage(this NetworkReader reader)
		{
			return new ClientRadioCommandMessage(reader);
		}

		public static readonly Dictionary<uint, RadioStatusMessage> SyncedRangeLevels = new Dictionary<uint, RadioStatusMessage>();

		public enum RadioCommand : byte
		{
			Enable,
			Disable,
			ChangeRange
		}

		public enum RadioRangeLevel : sbyte
		{
			RadioDisabled = -1,
			LowRange,
			MediumRange,
			HighRange,
			UltraRange
		}
	}
}
