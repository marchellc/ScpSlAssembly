using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace LiteNetLib;

internal sealed class NetPacket
{
	private static readonly int PropertiesCount;

	private static readonly int[] HeaderSizes;

	public byte[] RawData;

	public int Size;

	public object UserData;

	public NetPacket Next;

	public PacketProperty Property
	{
		get
		{
			return (PacketProperty)(this.RawData[0] & 0x1F);
		}
		set
		{
			this.RawData[0] = (byte)((uint)(this.RawData[0] & 0xE0) | (uint)value);
		}
	}

	public byte ConnectionNumber
	{
		get
		{
			return (byte)((this.RawData[0] & 0x60) >> 5);
		}
		set
		{
			this.RawData[0] = (byte)((this.RawData[0] & 0x9F) | (value << 5));
		}
	}

	public ushort Sequence
	{
		get
		{
			return BitConverter.ToUInt16(this.RawData, 1);
		}
		set
		{
			FastBitConverter.GetBytes(this.RawData, 1, value);
		}
	}

	public bool IsFragmented => (this.RawData[0] & 0x80) != 0;

	public byte ChannelId
	{
		get
		{
			return this.RawData[3];
		}
		set
		{
			this.RawData[3] = value;
		}
	}

	public ushort FragmentId
	{
		get
		{
			return BitConverter.ToUInt16(this.RawData, 4);
		}
		set
		{
			FastBitConverter.GetBytes(this.RawData, 4, value);
		}
	}

	public ushort FragmentPart
	{
		get
		{
			return BitConverter.ToUInt16(this.RawData, 6);
		}
		set
		{
			FastBitConverter.GetBytes(this.RawData, 6, value);
		}
	}

	public ushort FragmentsTotal
	{
		get
		{
			return BitConverter.ToUInt16(this.RawData, 8);
		}
		set
		{
			FastBitConverter.GetBytes(this.RawData, 8, value);
		}
	}

	static NetPacket()
	{
		NetPacket.PropertiesCount = Enum.GetValues(typeof(PacketProperty)).Length;
		NetPacket.HeaderSizes = NetUtils.AllocatePinnedUninitializedArray<int>(NetPacket.PropertiesCount);
		for (int i = 0; i < NetPacket.HeaderSizes.Length; i++)
		{
			switch ((PacketProperty)(byte)i)
			{
			case PacketProperty.Channeled:
			case PacketProperty.Ack:
				NetPacket.HeaderSizes[i] = 4;
				break;
			case PacketProperty.Ping:
				NetPacket.HeaderSizes[i] = 3;
				break;
			case PacketProperty.ConnectRequest:
				NetPacket.HeaderSizes[i] = 18;
				break;
			case PacketProperty.ConnectAccept:
				NetPacket.HeaderSizes[i] = 15;
				break;
			case PacketProperty.Disconnect:
				NetPacket.HeaderSizes[i] = 9;
				break;
			case PacketProperty.Pong:
				NetPacket.HeaderSizes[i] = 11;
				break;
			default:
				NetPacket.HeaderSizes[i] = 1;
				break;
			}
		}
	}

	public void MarkFragmented()
	{
		this.RawData[0] |= 128;
	}

	public NetPacket(int size)
	{
		this.RawData = new byte[size];
		this.Size = size;
	}

	public NetPacket(PacketProperty property, int size)
	{
		size += NetPacket.GetHeaderSize(property);
		this.RawData = new byte[size];
		this.Property = property;
		this.Size = size;
	}

	public static int GetHeaderSize(PacketProperty property)
	{
		return NetPacket.HeaderSizes[(uint)property];
	}

	public int GetHeaderSize()
	{
		return NetPacket.HeaderSizes[this.RawData[0] & 0x1F];
	}

	public bool Verify()
	{
		try
		{
			if (this.RawData.Length == 0)
			{
				return false;
			}
			byte b = (byte)(this.RawData[0] & 0x1F);
			if (b >= NetPacket.PropertiesCount)
			{
				return false;
			}
			int num = NetPacket.HeaderSizes[b];
			bool flag = (this.RawData[0] & 0x80) != 0;
			return this.Size >= num && (!flag || this.Size >= num + 6);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error verifying packet: {arg}");
			return false;
		}
	}
}
