using System;
using System.Net;

namespace LiteNetLib
{
	internal class NativeEndPoint : IPEndPoint
	{
		public NativeEndPoint(byte[] address)
			: base(IPAddress.Any, 0)
		{
			this.NativeAddress = new byte[address.Length];
			Buffer.BlockCopy(address, 0, this.NativeAddress, 0, address.Length);
			short num = (short)(((int)address[1] << 8) | (int)address[0]);
			base.Port = (int)((ushort)(((int)address[2] << 8) | (int)address[3]));
			if ((NativeSocket.UnixMode && num == 10) || (!NativeSocket.UnixMode && num == 23))
			{
				uint num2 = (uint)(((int)address[27] << 24) + ((int)address[26] << 16) + ((int)address[25] << 8) + (int)address[24]);
				byte[] array = new byte[16];
				Buffer.BlockCopy(address, 8, array, 0, 16);
				base.Address = new IPAddress(array, (long)((ulong)num2));
				return;
			}
			long num3 = (long)((ulong)((int)(address[4] & byte.MaxValue) | (((int)address[5] << 8) & 65280) | (((int)address[6] << 16) & 16711680) | ((int)address[7] << 24)));
			base.Address = new IPAddress(num3);
		}

		public readonly byte[] NativeAddress;
	}
}
