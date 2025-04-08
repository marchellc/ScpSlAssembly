﻿using System;
using System.Runtime.InteropServices;

namespace Utf8Json.Internal
{
	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	internal struct GuidBits
	{
		public GuidBits(ref Guid value)
		{
			this = default(GuidBits);
			this.Value = value;
		}

		public GuidBits(ref ArraySegment<byte> utf8string)
		{
			this = default(GuidBits);
			byte[] array = utf8string.Array;
			int offset = utf8string.Offset;
			if (utf8string.Count == 32)
			{
				if (BitConverter.IsLittleEndian)
				{
					this.Byte0 = GuidBits.Parse(array, offset + 6);
					this.Byte1 = GuidBits.Parse(array, offset + 4);
					this.Byte2 = GuidBits.Parse(array, offset + 2);
					this.Byte3 = GuidBits.Parse(array, offset);
					this.Byte4 = GuidBits.Parse(array, offset + 10);
					this.Byte5 = GuidBits.Parse(array, offset + 8);
					this.Byte6 = GuidBits.Parse(array, offset + 14);
					this.Byte7 = GuidBits.Parse(array, offset + 12);
				}
				else
				{
					this.Byte0 = GuidBits.Parse(array, offset);
					this.Byte1 = GuidBits.Parse(array, offset + 2);
					this.Byte2 = GuidBits.Parse(array, offset + 4);
					this.Byte3 = GuidBits.Parse(array, offset + 6);
					this.Byte4 = GuidBits.Parse(array, offset + 8);
					this.Byte5 = GuidBits.Parse(array, offset + 10);
					this.Byte6 = GuidBits.Parse(array, offset + 12);
					this.Byte7 = GuidBits.Parse(array, offset + 14);
				}
				this.Byte8 = GuidBits.Parse(array, offset + 16);
				this.Byte9 = GuidBits.Parse(array, offset + 18);
				this.Byte10 = GuidBits.Parse(array, offset + 20);
				this.Byte11 = GuidBits.Parse(array, offset + 22);
				this.Byte12 = GuidBits.Parse(array, offset + 24);
				this.Byte13 = GuidBits.Parse(array, offset + 26);
				this.Byte14 = GuidBits.Parse(array, offset + 28);
				this.Byte15 = GuidBits.Parse(array, offset + 30);
				return;
			}
			if (utf8string.Count == 36)
			{
				if (BitConverter.IsLittleEndian)
				{
					this.Byte0 = GuidBits.Parse(array, offset + 6);
					this.Byte1 = GuidBits.Parse(array, offset + 4);
					this.Byte2 = GuidBits.Parse(array, offset + 2);
					this.Byte3 = GuidBits.Parse(array, offset);
					if (array[offset + 8] != 45)
					{
						goto IL_0378;
					}
					this.Byte4 = GuidBits.Parse(array, offset + 11);
					this.Byte5 = GuidBits.Parse(array, offset + 9);
					if (array[offset + 13] != 45)
					{
						goto IL_0378;
					}
					this.Byte6 = GuidBits.Parse(array, offset + 16);
					this.Byte7 = GuidBits.Parse(array, offset + 14);
				}
				else
				{
					this.Byte0 = GuidBits.Parse(array, offset);
					this.Byte1 = GuidBits.Parse(array, offset + 2);
					this.Byte2 = GuidBits.Parse(array, offset + 4);
					this.Byte3 = GuidBits.Parse(array, offset + 6);
					if (array[offset + 8] != 45)
					{
						goto IL_0378;
					}
					this.Byte4 = GuidBits.Parse(array, offset + 9);
					this.Byte5 = GuidBits.Parse(array, offset + 11);
					if (array[offset + 13] != 45)
					{
						goto IL_0378;
					}
					this.Byte6 = GuidBits.Parse(array, offset + 14);
					this.Byte7 = GuidBits.Parse(array, offset + 16);
				}
				if (array[offset + 18] == 45)
				{
					this.Byte8 = GuidBits.Parse(array, offset + 19);
					this.Byte9 = GuidBits.Parse(array, offset + 21);
					if (array[offset + 23] == 45)
					{
						this.Byte10 = GuidBits.Parse(array, offset + 24);
						this.Byte11 = GuidBits.Parse(array, offset + 26);
						this.Byte12 = GuidBits.Parse(array, offset + 28);
						this.Byte13 = GuidBits.Parse(array, offset + 30);
						this.Byte14 = GuidBits.Parse(array, offset + 32);
						this.Byte15 = GuidBits.Parse(array, offset + 34);
						return;
					}
				}
			}
			IL_0378:
			throw new ArgumentException("Invalid Guid Pattern.");
		}

		private static byte Parse(byte[] bytes, int highOffset)
		{
			return GuidBits.SwitchParse(bytes[highOffset]) * 16 + GuidBits.SwitchParse(bytes[highOffset + 1]);
		}

		private static byte SwitchParse(byte b)
		{
			switch (b)
			{
			case 48:
			case 49:
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
			case 56:
			case 57:
				return b - 48;
			case 65:
			case 66:
			case 67:
			case 68:
			case 69:
			case 70:
				return b - 55;
			case 97:
			case 98:
			case 99:
			case 100:
			case 101:
			case 102:
				return b - 87;
			}
			throw new ArgumentException("Invalid Guid Pattern.");
		}

		public void Write(byte[] buffer, int offset)
		{
			if (BitConverter.IsLittleEndian)
			{
				buffer[offset + 6] = GuidBits.byteToHexStringHigh[(int)this.Byte0];
				buffer[offset + 7] = GuidBits.byteToHexStringLow[(int)this.Byte0];
				buffer[offset + 4] = GuidBits.byteToHexStringHigh[(int)this.Byte1];
				buffer[offset + 5] = GuidBits.byteToHexStringLow[(int)this.Byte1];
				buffer[offset + 2] = GuidBits.byteToHexStringHigh[(int)this.Byte2];
				buffer[offset + 3] = GuidBits.byteToHexStringLow[(int)this.Byte2];
				buffer[offset] = GuidBits.byteToHexStringHigh[(int)this.Byte3];
				buffer[offset + 1] = GuidBits.byteToHexStringLow[(int)this.Byte3];
				buffer[offset + 8] = 45;
				buffer[offset + 11] = GuidBits.byteToHexStringHigh[(int)this.Byte4];
				buffer[offset + 12] = GuidBits.byteToHexStringLow[(int)this.Byte4];
				buffer[offset + 9] = GuidBits.byteToHexStringHigh[(int)this.Byte5];
				buffer[offset + 10] = GuidBits.byteToHexStringLow[(int)this.Byte5];
				buffer[offset + 13] = 45;
				buffer[offset + 16] = GuidBits.byteToHexStringHigh[(int)this.Byte6];
				buffer[offset + 17] = GuidBits.byteToHexStringLow[(int)this.Byte6];
				buffer[offset + 14] = GuidBits.byteToHexStringHigh[(int)this.Byte7];
				buffer[offset + 15] = GuidBits.byteToHexStringLow[(int)this.Byte7];
			}
			else
			{
				buffer[offset] = GuidBits.byteToHexStringHigh[(int)this.Byte0];
				buffer[offset + 1] = GuidBits.byteToHexStringLow[(int)this.Byte0];
				buffer[offset + 2] = GuidBits.byteToHexStringHigh[(int)this.Byte1];
				buffer[offset + 3] = GuidBits.byteToHexStringLow[(int)this.Byte1];
				buffer[offset + 4] = GuidBits.byteToHexStringHigh[(int)this.Byte2];
				buffer[offset + 5] = GuidBits.byteToHexStringLow[(int)this.Byte2];
				buffer[offset + 6] = GuidBits.byteToHexStringHigh[(int)this.Byte3];
				buffer[offset + 7] = GuidBits.byteToHexStringLow[(int)this.Byte3];
				buffer[offset + 8] = 45;
				buffer[offset + 9] = GuidBits.byteToHexStringHigh[(int)this.Byte4];
				buffer[offset + 10] = GuidBits.byteToHexStringLow[(int)this.Byte4];
				buffer[offset + 11] = GuidBits.byteToHexStringHigh[(int)this.Byte5];
				buffer[offset + 12] = GuidBits.byteToHexStringLow[(int)this.Byte5];
				buffer[offset + 13] = 45;
				buffer[offset + 14] = GuidBits.byteToHexStringHigh[(int)this.Byte6];
				buffer[offset + 15] = GuidBits.byteToHexStringLow[(int)this.Byte6];
				buffer[offset + 16] = GuidBits.byteToHexStringHigh[(int)this.Byte7];
				buffer[offset + 17] = GuidBits.byteToHexStringLow[(int)this.Byte7];
			}
			buffer[offset + 18] = 45;
			buffer[offset + 19] = GuidBits.byteToHexStringHigh[(int)this.Byte8];
			buffer[offset + 20] = GuidBits.byteToHexStringLow[(int)this.Byte8];
			buffer[offset + 21] = GuidBits.byteToHexStringHigh[(int)this.Byte9];
			buffer[offset + 22] = GuidBits.byteToHexStringLow[(int)this.Byte9];
			buffer[offset + 23] = 45;
			buffer[offset + 24] = GuidBits.byteToHexStringHigh[(int)this.Byte10];
			buffer[offset + 25] = GuidBits.byteToHexStringLow[(int)this.Byte10];
			buffer[offset + 26] = GuidBits.byteToHexStringHigh[(int)this.Byte11];
			buffer[offset + 27] = GuidBits.byteToHexStringLow[(int)this.Byte11];
			buffer[offset + 28] = GuidBits.byteToHexStringHigh[(int)this.Byte12];
			buffer[offset + 29] = GuidBits.byteToHexStringLow[(int)this.Byte12];
			buffer[offset + 30] = GuidBits.byteToHexStringHigh[(int)this.Byte13];
			buffer[offset + 31] = GuidBits.byteToHexStringLow[(int)this.Byte13];
			buffer[offset + 32] = GuidBits.byteToHexStringHigh[(int)this.Byte14];
			buffer[offset + 33] = GuidBits.byteToHexStringLow[(int)this.Byte14];
			buffer[offset + 34] = GuidBits.byteToHexStringHigh[(int)this.Byte15];
			buffer[offset + 35] = GuidBits.byteToHexStringLow[(int)this.Byte15];
		}

		[FieldOffset(0)]
		public readonly Guid Value;

		[FieldOffset(0)]
		public readonly byte Byte0;

		[FieldOffset(1)]
		public readonly byte Byte1;

		[FieldOffset(2)]
		public readonly byte Byte2;

		[FieldOffset(3)]
		public readonly byte Byte3;

		[FieldOffset(4)]
		public readonly byte Byte4;

		[FieldOffset(5)]
		public readonly byte Byte5;

		[FieldOffset(6)]
		public readonly byte Byte6;

		[FieldOffset(7)]
		public readonly byte Byte7;

		[FieldOffset(8)]
		public readonly byte Byte8;

		[FieldOffset(9)]
		public readonly byte Byte9;

		[FieldOffset(10)]
		public readonly byte Byte10;

		[FieldOffset(11)]
		public readonly byte Byte11;

		[FieldOffset(12)]
		public readonly byte Byte12;

		[FieldOffset(13)]
		public readonly byte Byte13;

		[FieldOffset(14)]
		public readonly byte Byte14;

		[FieldOffset(15)]
		public readonly byte Byte15;

		private static byte[] byteToHexStringHigh = new byte[]
		{
			48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
			48, 48, 48, 48, 48, 48, 49, 49, 49, 49,
			49, 49, 49, 49, 49, 49, 49, 49, 49, 49,
			49, 49, 50, 50, 50, 50, 50, 50, 50, 50,
			50, 50, 50, 50, 50, 50, 50, 50, 51, 51,
			51, 51, 51, 51, 51, 51, 51, 51, 51, 51,
			51, 51, 51, 51, 52, 52, 52, 52, 52, 52,
			52, 52, 52, 52, 52, 52, 52, 52, 52, 52,
			53, 53, 53, 53, 53, 53, 53, 53, 53, 53,
			53, 53, 53, 53, 53, 53, 54, 54, 54, 54,
			54, 54, 54, 54, 54, 54, 54, 54, 54, 54,
			54, 54, 55, 55, 55, 55, 55, 55, 55, 55,
			55, 55, 55, 55, 55, 55, 55, 55, 56, 56,
			56, 56, 56, 56, 56, 56, 56, 56, 56, 56,
			56, 56, 56, 56, 57, 57, 57, 57, 57, 57,
			57, 57, 57, 57, 57, 57, 57, 57, 57, 57,
			97, 97, 97, 97, 97, 97, 97, 97, 97, 97,
			97, 97, 97, 97, 97, 97, 98, 98, 98, 98,
			98, 98, 98, 98, 98, 98, 98, 98, 98, 98,
			98, 98, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 100, 100,
			100, 100, 100, 100, 100, 100, 100, 100, 100, 100,
			100, 100, 100, 100, 101, 101, 101, 101, 101, 101,
			101, 101, 101, 101, 101, 101, 101, 101, 101, 101,
			102, 102, 102, 102, 102, 102, 102, 102, 102, 102,
			102, 102, 102, 102, 102, 102
		};

		private static byte[] byteToHexStringLow = new byte[]
		{
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
			97, 98, 99, 100, 101, 102, 48, 49, 50, 51,
			52, 53, 54, 55, 56, 57, 97, 98, 99, 100,
			101, 102, 48, 49, 50, 51, 52, 53, 54, 55,
			56, 57, 97, 98, 99, 100, 101, 102, 48, 49,
			50, 51, 52, 53, 54, 55, 56, 57, 97, 98,
			99, 100, 101, 102, 48, 49, 50, 51, 52, 53,
			54, 55, 56, 57, 97, 98, 99, 100, 101, 102,
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
			97, 98, 99, 100, 101, 102, 48, 49, 50, 51,
			52, 53, 54, 55, 56, 57, 97, 98, 99, 100,
			101, 102, 48, 49, 50, 51, 52, 53, 54, 55,
			56, 57, 97, 98, 99, 100, 101, 102, 48, 49,
			50, 51, 52, 53, 54, 55, 56, 57, 97, 98,
			99, 100, 101, 102, 48, 49, 50, 51, 52, 53,
			54, 55, 56, 57, 97, 98, 99, 100, 101, 102,
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
			97, 98, 99, 100, 101, 102, 48, 49, 50, 51,
			52, 53, 54, 55, 56, 57, 97, 98, 99, 100,
			101, 102, 48, 49, 50, 51, 52, 53, 54, 55,
			56, 57, 97, 98, 99, 100, 101, 102, 48, 49,
			50, 51, 52, 53, 54, 55, 56, 57, 97, 98,
			99, 100, 101, 102, 48, 49, 50, 51, 52, 53,
			54, 55, 56, 57, 97, 98, 99, 100, 101, 102,
			48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
			97, 98, 99, 100, 101, 102
		};
	}
}
