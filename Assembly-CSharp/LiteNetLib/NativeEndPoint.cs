using System;
using System.Net;

namespace LiteNetLib;

internal class NativeEndPoint : IPEndPoint
{
	public readonly byte[] NativeAddress;

	public NativeEndPoint(byte[] address)
		: base(IPAddress.Any, 0)
	{
		NativeAddress = new byte[address.Length];
		Buffer.BlockCopy(address, 0, NativeAddress, 0, address.Length);
		short num = (short)((address[1] << 8) | address[0]);
		base.Port = (ushort)((address[2] << 8) | address[3]);
		if ((NativeSocket.UnixMode && num == 10) || (!NativeSocket.UnixMode && num == 23))
		{
			uint num2 = (uint)((address[27] << 24) + (address[26] << 16) + (address[25] << 8) + address[24]);
			byte[] array = new byte[16];
			Buffer.BlockCopy(address, 8, array, 0, 16);
			base.Address = new IPAddress(array, num2);
		}
		else
		{
			long newAddress = (uint)((address[4] & 0xFF) | ((address[5] << 8) & 0xFF00) | ((address[6] << 16) & 0xFF0000) | (address[7] << 24));
			base.Address = new IPAddress(newAddress);
		}
	}
}
