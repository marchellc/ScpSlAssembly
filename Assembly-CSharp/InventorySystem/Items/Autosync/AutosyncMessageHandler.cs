using System;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Autosync
{
	public static class AutosyncMessageHandler
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<AutosyncMessage>(new Action<NetworkConnectionToClient, AutosyncMessage>(AutosyncMessageHandler.ServerHandleAutosyncCmd), true);
				NetworkClient.ReplaceHandler<AutosyncMessage>(new Action<AutosyncMessage>(AutosyncMessageHandler.ClientHandleAutosyncRpc), true);
			};
		}

		private static void ClientHandleAutosyncRpc(AutosyncMessage msg)
		{
			try
			{
				msg.ProcessRpc();
			}
			catch (Exception ex)
			{
				Debug.Log("Exception in AutoSync RPC handler for " + msg.ToString());
				Debug.LogException(ex);
			}
		}

		private static void ServerHandleAutosyncCmd(NetworkConnection conn, AutosyncMessage msg)
		{
			ReferenceHub referenceHub;
			if (conn.identity == null || !ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			msg.ProcessCmd(referenceHub);
		}

		public static AutosyncMessage ReadAutosyncMessage(this NetworkReader reader)
		{
			return new AutosyncMessage(reader);
		}

		public static void WriteAutosyncMessage(this NetworkWriter writer, AutosyncMessage msg)
		{
			msg.Serialize(writer);
		}
	}
}
