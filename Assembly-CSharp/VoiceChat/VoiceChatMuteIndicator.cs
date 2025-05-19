using Mirror;
using UnityEngine;

namespace VoiceChat;

public class VoiceChatMuteIndicator : MonoBehaviour
{
	public struct SyncMuteMessage : NetworkMessage
	{
		public byte Flags;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<SyncMuteMessage>(ReceiveMessage);
		};
		VoiceChatMutes.OnFlagsSet += delegate(ReferenceHub hub, VcMuteFlags flags)
		{
			if (NetworkServer.active)
			{
				hub.connectionToClient.Send(new SyncMuteMessage
				{
					Flags = (byte)flags
				});
			}
		};
	}

	private static void ReceiveMessage(SyncMuteMessage smm)
	{
	}
}
