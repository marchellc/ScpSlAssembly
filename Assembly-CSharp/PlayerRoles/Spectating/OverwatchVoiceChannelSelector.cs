using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using VoiceChat;

namespace PlayerRoles.Spectating;

public class OverwatchVoiceChannelSelector : MonoBehaviour
{
	private struct ChannelMuteFlagsMessage : NetworkMessage
	{
		public bool SpatialAudio;

		public uint EnabledChannels;
	}

	public static readonly CachedValue<VoiceChatChannel[]> AllChannels = new CachedValue<VoiceChatChannel[]>(() => Enum.GetValues(typeof(VoiceChatChannel)) as VoiceChatChannel[]);

	private static readonly HashSet<VoiceChatChannel> ActiveMutes = new HashSet<VoiceChatChannel>();

	private static uint SerializeChannels()
	{
		uint num = 0u;
		foreach (VoiceChatChannel activeMute in OverwatchVoiceChannelSelector.ActiveMutes)
		{
			uint num2 = 1u;
			for (int i = 0; i < (int)activeMute; i++)
			{
				num2 *= 2;
			}
			num += num2;
		}
		return num;
	}

	private static void DeserializeChannels(uint channels)
	{
		OverwatchVoiceChannelSelector.ActiveMutes.Clear();
		VoiceChatChannel[] value = OverwatchVoiceChannelSelector.AllChannels.Value;
		foreach (VoiceChatChannel voiceChatChannel in value)
		{
			uint num = 1u;
			for (int j = 0; j < (int)voiceChatChannel; j++)
			{
				num *= 2;
			}
			if ((channels & num) != 0)
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
			NetworkServer.ReplaceHandler<ChannelMuteFlagsMessage>(ProcessMessage);
		};
	}

	private static void ProcessMessage(NetworkConnection conn, ChannelMuteFlagsMessage msg)
	{
		if (!(conn.identity == null) && ReferenceHub.TryGetHubNetID(conn.identity.netId, out var hub) && hub.roleManager.CurrentRole is OverwatchRole { VoiceModule: OverwatchVoiceModule voiceModule })
		{
			OverwatchVoiceChannelSelector.DeserializeChannels(msg.EnabledChannels);
			voiceModule.DisabledChannels = OverwatchVoiceChannelSelector.ActiveMutes.ToArray();
			voiceModule.UseSpatialAudio = msg.SpatialAudio;
		}
	}
}
