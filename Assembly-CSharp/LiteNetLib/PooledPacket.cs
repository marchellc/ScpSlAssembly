using System;

namespace LiteNetLib
{
	public readonly ref struct PooledPacket
	{
		public byte[] Data
		{
			get
			{
				return this._packet.RawData;
			}
		}

		internal PooledPacket(NetPacket packet, int maxDataSize, byte channelNumber)
		{
			this._packet = packet;
			this.UserDataOffset = this._packet.GetHeaderSize();
			this._packet.Size = this.UserDataOffset;
			this.MaxUserDataSize = maxDataSize - this.UserDataOffset;
			this._channelNumber = channelNumber;
		}

		internal readonly NetPacket _packet;

		internal readonly byte _channelNumber;

		public readonly int MaxUserDataSize;

		public readonly int UserDataOffset;
	}
}
