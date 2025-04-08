﻿using System;
using System.Net;
using LiteNetLib.Utils;

namespace LiteNetLib
{
	internal sealed class NetConnectRequestPacket
	{
		private NetConnectRequestPacket(long connectionTime, byte connectionNumber, int localId, byte[] targetAddress, NetDataReader data)
		{
			this.ConnectionTime = connectionTime;
			this.ConnectionNumber = connectionNumber;
			this.TargetAddress = targetAddress;
			this.Data = data;
			this.PeerId = localId;
		}

		public static int GetProtocolId(NetPacket packet)
		{
			return BitConverter.ToInt32(packet.RawData, 1);
		}

		public static NetConnectRequestPacket FromData(NetPacket packet)
		{
			if (packet.ConnectionNumber >= 4)
			{
				return null;
			}
			long num = BitConverter.ToInt64(packet.RawData, 5);
			int num2 = BitConverter.ToInt32(packet.RawData, 13);
			int num3 = (int)packet.RawData[17];
			if (num3 != 16 && num3 != 28)
			{
				return null;
			}
			byte[] array = new byte[num3];
			Buffer.BlockCopy(packet.RawData, 18, array, 0, num3);
			NetDataReader netDataReader = new NetDataReader(null, 0, 0);
			if (packet.Size > 18 + num3)
			{
				netDataReader.SetSource(packet.RawData, 18 + num3, packet.Size);
			}
			return new NetConnectRequestPacket(num, packet.ConnectionNumber, num2, array, netDataReader);
		}

		public static NetPacket Make(NetDataWriter connectData, SocketAddress addressBytes, long connectTime, int localId)
		{
			NetPacket netPacket = new NetPacket(PacketProperty.ConnectRequest, connectData.Length + addressBytes.Size);
			FastBitConverter.GetBytes(netPacket.RawData, 1, 13);
			FastBitConverter.GetBytes(netPacket.RawData, 5, connectTime);
			FastBitConverter.GetBytes(netPacket.RawData, 13, localId);
			netPacket.RawData[17] = (byte)addressBytes.Size;
			for (int i = 0; i < addressBytes.Size; i++)
			{
				netPacket.RawData[18 + i] = addressBytes[i];
			}
			Buffer.BlockCopy(connectData.Data, 0, netPacket.RawData, 18 + addressBytes.Size, connectData.Length);
			return netPacket;
		}

		public const int HeaderSize = 18;

		public readonly long ConnectionTime;

		public byte ConnectionNumber;

		public readonly byte[] TargetAddress;

		public readonly NetDataReader Data;

		public readonly int PeerId;
	}
}
