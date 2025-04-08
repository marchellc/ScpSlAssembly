using System;
using Mirror;

namespace VoiceChat.Networking
{
	public struct AudioMessage : NetworkMessage
	{
		public AudioMessage(byte controllerId, byte[] data, int dataLen)
		{
			this.ControllerId = controllerId;
			this.Data = data;
			this.DataLength = dataLen;
		}

		public byte ControllerId;

		public int DataLength;

		public byte[] Data;
	}
}
