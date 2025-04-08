using System;
using VoiceChat;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507VoiceModule : StandardScpVoiceModule
	{
		protected override VoiceChatChannel PrimaryChannel
		{
			get
			{
				return VoiceChatChannel.PreGameLobby;
			}
		}
	}
}
