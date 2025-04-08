using System;
using VoiceChat;

namespace PlayerRoles.Voice
{
	public abstract class GlobalVoiceModuleBase : StandardVoiceModule
	{
		public override bool IsSpeaking
		{
			get
			{
				return base.GlobalChatActive;
			}
		}

		protected abstract VoiceChatChannel PrimaryChannel { get; }

		public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
		{
			if (channel != this.PrimaryChannel)
			{
				return VoiceChatChannel.None;
			}
			return channel;
		}

		protected override void ProcessSamples(float[] data, int len)
		{
			this.GlobalPlayback.Buffer.Write(data, len);
		}

		public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
		{
			channel = base.ValidateReceive(speaker, channel);
			if (channel == this.PrimaryChannel)
			{
				return channel;
			}
			if (channel - VoiceChatChannel.Proximity <= 1 || channel - VoiceChatChannel.RoundSummary <= 2)
			{
				return channel;
			}
			return VoiceChatChannel.None;
		}

		protected override VoiceChatChannel ProcessInputs(bool primary, bool alt)
		{
			if (!primary)
			{
				return VoiceChatChannel.None;
			}
			return this.PrimaryChannel;
		}
	}
}
