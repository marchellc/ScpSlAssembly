using VoiceChat;

namespace PlayerRoles.Voice;

public abstract class GlobalVoiceModuleBase : StandardVoiceModule
{
	public override bool IsSpeaking => base.GlobalChatActive;

	protected abstract VoiceChatChannel PrimaryChannel { get; }

	public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
	{
		if (channel != PrimaryChannel)
		{
			return VoiceChatChannel.None;
		}
		return channel;
	}

	protected override void ProcessSamples(float[] data, int len)
	{
		GlobalPlayback.Buffer.Write(data, len);
	}

	public override VoiceChatChannel ValidateReceive(ReferenceHub speaker, VoiceChatChannel channel)
	{
		channel = base.ValidateReceive(speaker, channel);
		if (channel == PrimaryChannel)
		{
			return channel;
		}
		if ((uint)(channel - 1) <= 1u || (uint)(channel - 5) <= 2u)
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
		return PrimaryChannel;
	}
}
