using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;

namespace VoiceChat.Networking;

public static class VoiceTransceiver
{
	public delegate void VoiceMessageReceiving(VoiceMessage message, ReferenceHub hub);

	private static readonly List<OpusEncoder> Encoders = new List<OpusEncoder>();

	private static int _encodersCount;

	private static int _packageSize;

	private static float[] _sendBuffer;

	private static byte[] _encodedBuffer;

	public static event VoiceMessageReceiving OnVoiceMessageReceiving;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkServer.ReplaceHandler<VoiceMessage>(ServerReceiveMessage);
			NetworkClient.ReplaceHandler<VoiceMessage>(ClientReceiveMessage);
		};
		VoiceTransceiver._packageSize = 480;
		VoiceTransceiver._sendBuffer = new float[VoiceTransceiver._packageSize];
		VoiceTransceiver._encodedBuffer = new byte[512];
	}

	private static void ServerReceiveMessage(NetworkConnection conn, VoiceMessage msg)
	{
		if (msg.SpeakerNull || msg.Speaker.netId != conn.identity.netId || !(msg.Speaker.roleManager.CurrentRole is IVoiceRole voiceRole) || !voiceRole.VoiceModule.CheckRateLimit() || VoiceChatMutes.IsMuted(msg.Speaker))
		{
			return;
		}
		VoiceChatChannel voiceChatChannel = voiceRole.VoiceModule.ValidateSend(msg.Channel);
		if (voiceChatChannel == VoiceChatChannel.None)
		{
			return;
		}
		voiceRole.VoiceModule.CurrentChannel = voiceChatChannel;
		PlayerSendingVoiceMessageEventArgs e = new PlayerSendingVoiceMessageEventArgs(ref msg);
		PlayerEvents.OnSendingVoiceMessage(e);
		if (!e.IsAllowed)
		{
			return;
		}
		msg = e.Message;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!(allHub.roleManager.CurrentRole is IVoiceRole voiceRole2))
			{
				continue;
			}
			VoiceChatChannel channel = voiceRole2.VoiceModule.ValidateReceive(msg.Speaker, voiceChatChannel);
			msg.Channel = channel;
			PlayerReceivingVoiceMessageEventArgs e2 = new PlayerReceivingVoiceMessageEventArgs(allHub, ref msg);
			PlayerEvents.OnReceivingVoiceMessage(e2);
			if (e2.IsAllowed)
			{
				msg = e2.Message;
				if (msg.Channel != VoiceChatChannel.None)
				{
					VoiceTransceiver.OnVoiceMessageReceiving?.Invoke(msg, allHub);
					allHub.connectionToClient.Send(msg);
				}
			}
		}
	}

	private static void ClientReceiveMessage(VoiceMessage msg)
	{
		if (!msg.SpeakerNull && msg.Speaker.roleManager.CurrentRole is IVoiceRole voiceRole)
		{
			voiceRole.VoiceModule.ProcessMessage(msg);
		}
	}

	public static void ClientSendData(PlaybackBuffer micBuffer, VoiceChatChannel targetChannel, int encoderId = 0)
	{
		if (ReferenceHub.TryGetLocalHub(out var hub))
		{
			while (VoiceTransceiver._encodersCount <= encoderId)
			{
				VoiceTransceiver.Encoders.Add(new OpusEncoder(OpusApplicationType.Voip));
				VoiceTransceiver._encodersCount++;
			}
			OpusEncoder opusEncoder = VoiceTransceiver.Encoders[encoderId];
			while (micBuffer.Length >= VoiceTransceiver._packageSize)
			{
				micBuffer.ReadTo(VoiceTransceiver._sendBuffer, VoiceTransceiver._packageSize, 0L);
				int dataLen = opusEncoder.Encode(VoiceTransceiver._sendBuffer, VoiceTransceiver._encodedBuffer);
				NetworkClient.Send(new VoiceMessage(hub, targetChannel, VoiceTransceiver._encodedBuffer, dataLen, isNull: false));
			}
		}
	}
}
