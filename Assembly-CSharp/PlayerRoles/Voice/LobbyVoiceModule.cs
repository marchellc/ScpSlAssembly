using CentralAuth;
using PlayerRoles.Spectating;
using VoiceChat;

namespace PlayerRoles.Voice;

public class LobbyVoiceModule : SpectatorVoiceModule
{
	protected override VoiceChatChannel PrimaryChannel => VoiceChatChannel.PreGameLobby;

	private VoiceChatChannel ValidateAuth(VoiceChatChannel channelToValidate)
	{
		if (base.Owner.authManager.InstanceMode != ClientInstanceMode.Unverified)
		{
			return channelToValidate;
		}
		return VoiceChatChannel.None;
	}

	public override VoiceChatChannel ValidateSend(VoiceChatChannel channel)
	{
		return this.ValidateAuth(base.ValidateSend(channel));
	}

	public override VoiceChatChannel ValidateReceive(ReferenceHub sp, VoiceChatChannel ch)
	{
		if (base.Owner.isLocalPlayer)
		{
			return VoiceChatChannel.None;
		}
		if ((base.ReceiveFlags & GroupMuteFlags.Lobby) != GroupMuteFlags.None)
		{
			return VoiceChatChannel.None;
		}
		return this.ValidateAuth(base.ValidateReceive(sp, ch));
	}
}
