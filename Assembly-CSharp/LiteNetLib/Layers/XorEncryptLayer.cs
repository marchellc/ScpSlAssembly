using System;
using System.Net;
using System.Text;

namespace LiteNetLib.Layers;

public class XorEncryptLayer : PacketLayerBase
{
	private byte[] _byteKey;

	public XorEncryptLayer()
		: base(0)
	{
	}

	public XorEncryptLayer(byte[] key)
		: this()
	{
		this.SetKey(key);
	}

	public XorEncryptLayer(string key)
		: this()
	{
		this.SetKey(key);
	}

	public void SetKey(string key)
	{
		this._byteKey = Encoding.UTF8.GetBytes(key);
	}

	public void SetKey(byte[] key)
	{
		if (this._byteKey == null || this._byteKey.Length != key.Length)
		{
			this._byteKey = new byte[key.Length];
		}
		Buffer.BlockCopy(key, 0, this._byteKey, 0, key.Length);
	}

	public override void ProcessInboundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
	{
		if (this._byteKey != null)
		{
			int num = offset;
			int num2 = 0;
			while (num2 < length)
			{
				data[num] ^= this._byteKey[num2 % this._byteKey.Length];
				num2++;
				num++;
			}
		}
	}

	public override void ProcessOutBoundPacket(ref IPEndPoint endPoint, ref byte[] data, ref int offset, ref int length)
	{
		if (this._byteKey != null)
		{
			int num = offset;
			int num2 = 0;
			while (num2 < length)
			{
				data[num] ^= this._byteKey[num2 % this._byteKey.Length];
				num2++;
				num++;
			}
		}
	}
}
