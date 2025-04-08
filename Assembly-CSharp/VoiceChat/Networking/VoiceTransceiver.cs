using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;

namespace VoiceChat.Networking
{
	public static class VoiceTransceiver
	{
		public static event VoiceTransceiver.VoiceMessageReceiving OnVoiceMessageReceiving;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<VoiceMessage>(new Action<NetworkConnectionToClient, VoiceMessage>(VoiceTransceiver.ServerReceiveMessage), true);
				NetworkClient.ReplaceHandler<VoiceMessage>(new Action<VoiceMessage>(VoiceTransceiver.ClientReceiveMessage), true);
			};
			VoiceTransceiver._packageSize = 480;
			VoiceTransceiver._sendBuffer = new float[VoiceTransceiver._packageSize];
			VoiceTransceiver._encodedBuffer = new byte[512];
		}

		private static void ServerReceiveMessage(NetworkConnection conn, VoiceMessage msg)
		{
			if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId)
			{
				return;
			}
			IVoiceRole voiceRole = msg.Speaker.roleManager.CurrentRole as IVoiceRole;
			if (voiceRole == null)
			{
				return;
			}
			if (!voiceRole.VoiceModule.CheckRateLimit())
			{
				return;
			}
			if (VoiceChatMutes.IsMuted(msg.Speaker, false))
			{
				return;
			}
			VoiceChatChannel voiceChatChannel = voiceRole.VoiceModule.ValidateSend(msg.Channel);
			if (voiceChatChannel == VoiceChatChannel.None)
			{
				return;
			}
			voiceRole.VoiceModule.CurrentChannel = voiceChatChannel;
			PlayerSendingVoiceMessageEventArgs playerSendingVoiceMessageEventArgs = new PlayerSendingVoiceMessageEventArgs(msg);
			PlayerEvents.OnSendingVoiceMessage(playerSendingVoiceMessageEventArgs);
			if (!playerSendingVoiceMessageEventArgs.IsAllowed)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IVoiceRole voiceRole2 = referenceHub.roleManager.CurrentRole as IVoiceRole;
				if (voiceRole2 != null)
				{
					VoiceChatChannel voiceChatChannel2 = voiceRole2.VoiceModule.ValidateReceive(msg.Speaker, voiceChatChannel);
					msg.Channel = voiceChatChannel2;
					PlayerReceivingVoiceMessageEventArgs playerReceivingVoiceMessageEventArgs = new PlayerReceivingVoiceMessageEventArgs(referenceHub, msg);
					PlayerEvents.OnReceivingVoiceMessage(playerReceivingVoiceMessageEventArgs);
					if (playerReceivingVoiceMessageEventArgs.IsAllowed && msg.Channel != VoiceChatChannel.None)
					{
						VoiceTransceiver.VoiceMessageReceiving onVoiceMessageReceiving = VoiceTransceiver.OnVoiceMessageReceiving;
						if (onVoiceMessageReceiving != null)
						{
							onVoiceMessageReceiving(msg, referenceHub);
						}
						referenceHub.connectionToClient.Send<VoiceMessage>(msg, 0);
					}
				}
			}
		}

		private static void ClientReceiveMessage(VoiceMessage msg)
		{
			if (msg.SpeakerNull)
			{
				return;
			}
			IVoiceRole voiceRole = msg.Speaker.roleManager.CurrentRole as IVoiceRole;
			if (voiceRole != null)
			{
				voiceRole.VoiceModule.ProcessMessage(msg);
			}
		}

		public static void ClientSendData(PlaybackBuffer micBuffer, VoiceChatChannel targetChannel, int encoderId = 0)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			while (VoiceTransceiver._encodersCount <= encoderId)
			{
				VoiceTransceiver.Encoders.Add(new OpusEncoder(OpusApplicationType.Voip));
				VoiceTransceiver._encodersCount++;
			}
			OpusEncoder opusEncoder = VoiceTransceiver.Encoders[encoderId];
			while (micBuffer.Length >= VoiceTransceiver._packageSize)
			{
				micBuffer.ReadTo(VoiceTransceiver._sendBuffer, (long)VoiceTransceiver._packageSize, 0L);
				int num = opusEncoder.Encode(VoiceTransceiver._sendBuffer, VoiceTransceiver._encodedBuffer, 480);
				NetworkClient.Send<VoiceMessage>(new VoiceMessage(referenceHub, targetChannel, VoiceTransceiver._encodedBuffer, num, false), 0);
			}
		}

		private static readonly List<OpusEncoder> Encoders = new List<OpusEncoder>();

		private static int _encodersCount;

		private static int _packageSize;

		private static float[] _sendBuffer;

		private static byte[] _encodedBuffer;

		public delegate void VoiceMessageReceiving(VoiceMessage message, ReferenceHub hub);
	}
}
