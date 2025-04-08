using System;
using Mirror;
using UnityEngine;

namespace VoiceChat
{
	public class VoiceChatMuteIndicator : MonoBehaviour
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<VoiceChatMuteIndicator.SyncMuteMessage>(new Action<VoiceChatMuteIndicator.SyncMuteMessage>(VoiceChatMuteIndicator.ReceiveMessage), true);
			};
			VoiceChatMutes.OnFlagsSet += delegate(ReferenceHub hub, VcMuteFlags flags)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				hub.connectionToClient.Send<VoiceChatMuteIndicator.SyncMuteMessage>(new VoiceChatMuteIndicator.SyncMuteMessage
				{
					Flags = (byte)flags
				}, 0);
			};
		}

		private static void ReceiveMessage(VoiceChatMuteIndicator.SyncMuteMessage smm)
		{
		}

		public struct SyncMuteMessage : NetworkMessage
		{
			public byte Flags;
		}
	}
}
