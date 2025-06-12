using UnityEngine;
using VoiceChat;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079VoiceModule : StandardScpVoiceModule
{
	private Scp079SpeakerAbility _speakerAbility;

	public const VoiceChatChannel SpeakerChannel = VoiceChatChannel.Proximity;

	[field: SerializeField]
	public SingleBufferPlayback ProximityPlayback { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		(base.Role as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079SpeakerAbility>(out this._speakerAbility);
	}

	protected override VoiceChatChannel ProcessInputs(bool primary, bool alt)
	{
		if (alt && this._speakerAbility.CanTransmit)
		{
			return VoiceChatChannel.Proximity;
		}
		if (primary)
		{
			return this.PrimaryChannel;
		}
		return VoiceChatChannel.None;
	}

	public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
	{
		if (channel != VoiceChatChannel.Proximity || !this._speakerAbility.CanTransmit)
		{
			return base.ValidateSend(channel);
		}
		return VoiceChatChannel.Proximity;
	}

	protected override void ProcessSamples(float[] data, int len)
	{
		if (base.CurrentChannel == VoiceChatChannel.Proximity)
		{
			this.ProximityPlayback.Buffer.Write(data, len);
		}
		else
		{
			base.ProcessSamples(data, len);
		}
	}
}
