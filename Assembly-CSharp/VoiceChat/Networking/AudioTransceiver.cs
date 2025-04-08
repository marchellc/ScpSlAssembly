using System;
using Mirror;
using UnityEngine;
using VoiceChat.Playbacks;

namespace VoiceChat.Networking
{
	public static class AudioTransceiver
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<AudioMessage>(new Action<AudioMessage>(AudioTransceiver.ClientReceiveMessage), true);
			};
		}

		private static void ClientReceiveMessage(AudioMessage message)
		{
			foreach (SpeakerToyPlaybackBase speakerToyPlaybackBase in SpeakerToyPlaybackBase.AllInstances)
			{
				if (speakerToyPlaybackBase.ControllerId == message.ControllerId && !speakerToyPlaybackBase.Culled)
				{
					float maxDistance = speakerToyPlaybackBase.Source.maxDistance;
					if ((speakerToyPlaybackBase.LastPosition - MainCameraController.CurrentCamera.position).sqrMagnitude <= maxDistance * maxDistance)
					{
						speakerToyPlaybackBase.DecodeSamples(message);
					}
				}
			}
		}
	}
}
