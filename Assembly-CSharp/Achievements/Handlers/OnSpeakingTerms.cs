using System;
using PlayerRoles;
using VoiceChat;
using VoiceChat.Networking;

namespace Achievements.Handlers
{
	public class OnSpeakingTerms : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			VoiceTransceiver.OnVoiceMessageReceiving += OnSpeakingTerms.OnVoiceMessageReceiving;
		}

		private static void OnVoiceMessageReceiving(VoiceMessage message, ReferenceHub hub)
		{
			if (message.Channel != VoiceChatChannel.Radio)
			{
				return;
			}
			if (message.Speaker.GetTeam() != Team.ChaosInsurgency || hub.GetTeam() != Team.FoundationForces)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(message.Speaker.connectionToClient, AchievementName.OnSpeakingTerms);
		}
	}
}
