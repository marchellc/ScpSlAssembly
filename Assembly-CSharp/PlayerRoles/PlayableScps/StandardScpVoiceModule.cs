using System;
using PlayerRoles.Voice;
using VoiceChat;

namespace PlayerRoles.PlayableScps
{
	public class StandardScpVoiceModule : GlobalVoiceModuleBase
	{
		protected override VoiceChatChannel PrimaryChannel
		{
			get
			{
				return VoiceChatChannel.ScpChat;
			}
		}
	}
}
