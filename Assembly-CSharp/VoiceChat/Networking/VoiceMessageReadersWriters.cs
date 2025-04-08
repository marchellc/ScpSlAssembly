using System;
using Mirror;

namespace VoiceChat.Networking
{
	public static class VoiceMessageReadersWriters
	{
		public static VoiceMessage DeserializeVoiceMessage(this NetworkReader reader)
		{
			int value = reader.ReadRecyclablePlayerId().Value;
			VoiceChatChannel voiceChatChannel = (VoiceChatChannel)reader.ReadByte();
			int num = (int)reader.ReadUShort();
			reader.ReadBytes(VoiceMessageReadersWriters.ReceiveArray, num);
			ReferenceHub referenceHub;
			if (value == 0 || !ReferenceHub.TryGetHub(value, out referenceHub))
			{
				return new VoiceMessage(null, voiceChatChannel, VoiceMessageReadersWriters.ReceiveArray, num, true);
			}
			return new VoiceMessage(referenceHub, voiceChatChannel, VoiceMessageReadersWriters.ReceiveArray, num, false);
		}

		public static void SerializeVoiceMessage(this NetworkWriter writer, VoiceMessage msg)
		{
			writer.WriteRecyclablePlayerId(new RecyclablePlayerId(msg.Speaker.PlayerId));
			writer.WriteByte((byte)msg.Channel);
			writer.WriteUShort((ushort)msg.DataLength);
			writer.WriteBytes(msg.Data, 0, msg.DataLength);
		}

		private static readonly byte[] ReceiveArray = new byte[512];
	}
}
