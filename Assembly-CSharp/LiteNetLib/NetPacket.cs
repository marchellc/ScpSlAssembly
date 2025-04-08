using System;
using LiteNetLib.Utils;
using UnityEngine;

namespace LiteNetLib
{
	internal sealed class NetPacket
	{
		static NetPacket()
		{
			for (int i = 0; i < NetPacket.HeaderSizes.Length; i++)
			{
				switch ((byte)i)
				{
				case 1:
				case 2:
					NetPacket.HeaderSizes[i] = 4;
					break;
				case 3:
					NetPacket.HeaderSizes[i] = 3;
					break;
				case 4:
					NetPacket.HeaderSizes[i] = 11;
					break;
				case 5:
					NetPacket.HeaderSizes[i] = 18;
					break;
				case 6:
					NetPacket.HeaderSizes[i] = 15;
					break;
				case 7:
					NetPacket.HeaderSizes[i] = 9;
					break;
				default:
					NetPacket.HeaderSizes[i] = 1;
					break;
				}
			}
		}

		public PacketProperty Property
		{
			get
			{
				return (PacketProperty)(this.RawData[0] & 31);
			}
			set
			{
				this.RawData[0] = (this.RawData[0] & 224) | (byte)value;
			}
		}

		public byte ConnectionNumber
		{
			get
			{
				return (byte)((this.RawData[0] & 96) >> 5);
			}
			set
			{
				this.RawData[0] = (byte)((int)(this.RawData[0] & 159) | ((int)value << 5));
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

		public bool IsFragmented
		{
			get
			{
				return (this.RawData[0] & 128) > 0;
			}
		}

		public void MarkFragmented()
		{
			byte[] rawData = this.RawData;
			int num = 0;
			rawData[num] |= 128;
		}

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
			return NetPacket.HeaderSizes[(int)property];
		}

		public int GetHeaderSize()
		{
			return NetPacket.HeaderSizes[(int)(this.RawData[0] & 31)];
		}

		public bool Verify()
		{
			bool flag;
			try
			{
				if (this.RawData.Length == 0)
				{
					flag = false;
				}
				else
				{
					byte b = this.RawData[0] & 31;
					if ((int)b >= NetPacket.PropertiesCount)
					{
						flag = false;
					}
					else
					{
						int num = NetPacket.HeaderSizes[(int)b];
						bool flag2 = (this.RawData[0] & 128) > 0;
						flag = this.Size >= num && (!flag2 || this.Size >= num + 6);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(string.Format("Error verifying packet: {0}", ex));
				flag = false;
			}
			return flag;
		}

		private static readonly int PropertiesCount = Enum.GetValues(typeof(PacketProperty)).Length;

		private static readonly int[] HeaderSizes = NetUtils.AllocatePinnedUninitializedArray<int>(NetPacket.PropertiesCount);

		public byte[] RawData;

		public int Size;

		public object UserData;

		public NetPacket Next;
	}
}
