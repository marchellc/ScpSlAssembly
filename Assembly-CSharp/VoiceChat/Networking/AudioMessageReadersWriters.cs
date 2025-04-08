using System;
using Mirror;

namespace VoiceChat.Networking
{
	public static class AudioMessageReadersWriters
	{
		public static AudioMessage DeserializeVoiceMessage(this NetworkReader reader)
		{
			byte b = reader.ReadByte();
			int num = (int)reader.ReadUShort();
			reader.ReadBytes(AudioMessageReadersWriters.ReceiveArray, num);
			return new AudioMessage(b, AudioMessageReadersWriters.ReceiveArray, num);
		}

		public static void SerializeVoiceMessage(this NetworkWriter writer, AudioMessage msg)
		{
			writer.WriteByte(msg.ControllerId);
			writer.WriteUShort((ushort)msg.DataLength);
			writer.WriteBytes(msg.Data, 0, msg.DataLength);
		}

		private static readonly byte[] ReceiveArray = new byte[512];
	}
}
