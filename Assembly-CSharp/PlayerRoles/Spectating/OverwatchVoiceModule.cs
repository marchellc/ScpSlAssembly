using System;
using VoiceChat;

namespace PlayerRoles.Spectating
{
	public class OverwatchVoiceModule : SpectatorVoiceModule
	{
		public VoiceChatChannel[] DisabledChannels { get; set; } = Array.Empty<VoiceChatChannel>();

		public bool UseSpatialAudio { get; set; } = true;

		public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
		{
			if (channel != VoiceChatChannel.Scp1576 && channel != VoiceChatChannel.ScpChat)
			{
				channel = base.ValidateReceive(speaker, channel);
			}
			if (!this.UseSpatialAudio && (channel == VoiceChatChannel.Proximity || channel == VoiceChatChannel.Mimicry || channel == VoiceChatChannel.Radio) && !this.DisabledChannels.Contains(channel))
			{
				return VoiceChatChannel.RoundSummary;
			}
			if (!this.DisabledChannels.Contains(channel))
			{
				return channel;
			}
			return VoiceChatChannel.None;
		}
	}
}
