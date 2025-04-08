using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using VoiceChat;

namespace PlayerRoles.Spectating
{
	public class OverwatchVoiceChannelSelector : MonoBehaviour
	{
		private static uint SerializeChannels()
		{
			uint num = 0U;
			foreach (VoiceChatChannel voiceChatChannel in OverwatchVoiceChannelSelector.ActiveMutes)
			{
				uint num2 = 1U;
				for (int i = 0; i < (int)voiceChatChannel; i++)
				{
					num2 *= 2U;
				}
				num += num2;
			}
			return num;
		}

		private static void DeserializeChannels(uint channels)
		{
			OverwatchVoiceChannelSelector.ActiveMutes.Clear();
			foreach (VoiceChatChannel voiceChatChannel in OverwatchVoiceChannelSelector.AllChannels.Value)
			{
				uint num = 1U;
				for (int j = 0; j < (int)voiceChatChannel; j++)
				{
					num *= 2U;
				}
				if ((channels & num) != 0U)
				{
					OverwatchVoiceChannelSelector.ActiveMutes.Add(voiceChatChannel);
				}
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>(new Action<NetworkConnectionToClient, OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage>(OverwatchVoiceChannelSelector.ProcessMessage), true);
			};
		}

		private static void ProcessMessage(NetworkConnection conn, OverwatchVoiceChannelSelector.ChannelMuteFlagsMessage msg)
		{
			if (conn.identity == null)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			OverwatchRole overwatchRole = referenceHub.roleManager.CurrentRole as OverwatchRole;
			if (overwatchRole == null)
			{
				return;
			}
			OverwatchVoiceModule overwatchVoiceModule = overwatchRole.VoiceModule as OverwatchVoiceModule;
			if (overwatchVoiceModule == null)
			{
				return;
			}
			OverwatchVoiceChannelSelector.DeserializeChannels(msg.EnabledChannels);
			overwatchVoiceModule.DisabledChannels = OverwatchVoiceChannelSelector.ActiveMutes.ToArray<VoiceChatChannel>();
			overwatchVoiceModule.UseSpatialAudio = msg.SpatialAudio;
		}

		public static readonly CachedValue<VoiceChatChannel[]> AllChannels = new CachedValue<VoiceChatChannel[]>(() => Enum.GetValues(typeof(VoiceChatChannel)) as VoiceChatChannel[]);

		private static readonly HashSet<VoiceChatChannel> ActiveMutes = new HashSet<VoiceChatChannel>();

		private struct ChannelMuteFlagsMessage : NetworkMessage
		{
			public bool SpatialAudio;

			public uint EnabledChannels;
		}
	}
}
