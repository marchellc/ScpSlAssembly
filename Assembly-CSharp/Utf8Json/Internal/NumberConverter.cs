using System;
using Utf8Json.Internal.DoubleConversion;

namespace Utf8Json.Internal
{
	public static class NumberConverter
	{
		public static bool IsNumber(byte c)
		{
			return 48 <= c && c <= 57;
		}

		public static bool IsNumberRepresentation(byte c)
		{
			switch (c)
			{
			case 43:
			case 45:
			case 46:
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
				return true;
			}
			return false;
		}

		public static sbyte ReadSByte(byte[] bytes, int offset, out int readCount)
		{
			return checked((sbyte)NumberConverter.ReadInt64(bytes, offset, out readCount));
		}

		public static short ReadInt16(byte[] bytes, int offset, out int readCount)
		{
			return checked((short)NumberConverter.ReadInt64(bytes, offset, out readCount));
		}

		public static int ReadInt32(byte[] bytes, int offset, out int readCount)
		{
			return checked((int)NumberConverter.ReadInt64(bytes, offset, out readCount));
		}

		public static long ReadInt64(byte[] bytes, int offset, out int readCount)
		{
			long num = 0L;
			int num2 = 1;
			if (bytes[offset] == 45)
			{
				num2 = -1;
			}
			for (int i = ((num2 == -1) ? (offset + 1) : offset); i < bytes.Length; i++)
			{
				if (!NumberConverter.IsNumber(bytes[i]))
				{
					readCount = i - offset;
					IL_004B:
					return num * (long)num2;
				}
				num = num * 10L + (long)(bytes[i] - 48);
			}
			readCount = bytes.Length - offset;
			goto IL_004B;
		}

		public static byte ReadByte(byte[] bytes, int offset, out int readCount)
		{
			return checked((byte)NumberConverter.ReadUInt64(bytes, offset, out readCount));
		}

		public static ushort ReadUInt16(byte[] bytes, int offset, out int readCount)
		{
			return checked((ushort)NumberConverter.ReadUInt64(bytes, offset, out readCount));
		}

		public static uint ReadUInt32(byte[] bytes, int offset, out int readCount)
		{
			return checked((uint)NumberConverter.ReadUInt64(bytes, offset, out readCount));
		}

		public static ulong ReadUInt64(byte[] bytes, int offset, out int readCount)
		{
			ulong num = 0UL;
			for (int i = offset; i < bytes.Length; i++)
			{
				if (!NumberConverter.IsNumber(bytes[i]))
				{
					readCount = i - offset;
					return num;
				}
				num = checked(num * 10UL + (ulong)(bytes[i] - 48));
			}
			readCount = bytes.Length - offset;
			return num;
		}

		public static float ReadSingle(byte[] bytes, int offset, out int readCount)
		{
			return StringToDoubleConverter.ToSingle(bytes, offset, out readCount);
		}

		public static double ReadDouble(byte[] bytes, int offset, out int readCount)
		{
			return StringToDoubleConverter.ToDouble(bytes, offset, out readCount);
		}

		public static int WriteByte(ref byte[] buffer, int offset, byte value)
		{
			return NumberConverter.WriteUInt64(ref buffer, offset, (ulong)value);
		}

		public static int WriteUInt16(ref byte[] buffer, int offset, ushort value)
		{
			return NumberConverter.WriteUInt64(ref buffer, offset, (ulong)value);
		}

		public static int WriteUInt32(ref byte[] buffer, int offset, uint value)
		{
			return NumberConverter.WriteUInt64(ref buffer, offset, (ulong)value);
		}

		public static int WriteUInt64(ref byte[] buffer, int offset, ulong value)
		{
			int num = offset;
			ulong num2 = value;
			ulong num7;
			if (num2 < 10000UL)
			{
				if (num2 < 10UL)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 1);
					goto IL_0488;
				}
				if (num2 < 100UL)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 2);
					goto IL_0463;
				}
				if (num2 < 1000UL)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 3);
					goto IL_043E;
				}
				BinaryUtil.EnsureCapacity(ref buffer, offset, 4);
			}
			else
			{
				ulong num3 = num2 / 10000UL;
				num2 -= num3 * 10000UL;
				if (num3 < 10000UL)
				{
					if (num3 < 10UL)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 5);
						goto IL_0407;
					}
					if (num3 < 100UL)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 6);
						goto IL_03E2;
					}
					if (num3 < 1000UL)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 7);
						goto IL_03BD;
					}
					BinaryUtil.EnsureCapacity(ref buffer, offset, 8);
				}
				else
				{
					ulong num4 = num3 / 10000UL;
					num3 -= num4 * 10000UL;
					if (num4 < 10000UL)
					{
						if (num4 < 10UL)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 9);
							goto IL_0386;
						}
						if (num4 < 100UL)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 10);
							goto IL_0361;
						}
						if (num4 < 1000UL)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 11);
							goto IL_033C;
						}
						BinaryUtil.EnsureCapacity(ref buffer, offset, 12);
					}
					else
					{
						ulong num5 = num4 / 10000UL;
						num4 -= num5 * 10000UL;
						if (num5 < 10000UL)
						{
							if (num5 < 10UL)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 13);
								goto IL_0304;
							}
							if (num5 < 100UL)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 14);
								goto IL_02DC;
							}
							if (num5 < 1000UL)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 15);
								goto IL_02B4;
							}
							BinaryUtil.EnsureCapacity(ref buffer, offset, 16);
						}
						else
						{
							ulong num6 = num5 / 10000UL;
							num5 -= num6 * 10000UL;
							if (num6 < 10000UL)
							{
								if (num6 < 10UL)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 17);
									goto IL_0279;
								}
								if (num6 < 100UL)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 18);
									goto IL_0251;
								}
								if (num6 < 1000UL)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 19);
									goto IL_0229;
								}
								BinaryUtil.EnsureCapacity(ref buffer, offset, 20);
							}
							buffer[offset++] = (byte)(48UL + (num7 = num6 * 8389UL >> 23));
							num6 -= num7 * 1000UL;
							IL_0229:
							buffer[offset++] = (byte)(48UL + (num7 = num6 * 5243UL >> 19));
							num6 -= num7 * 100UL;
							IL_0251:
							buffer[offset++] = (byte)(48UL + (num7 = num6 * 6554UL >> 16));
							num6 -= num7 * 10UL;
							IL_0279:
							buffer[offset++] = (byte)(48UL + num6);
						}
						buffer[offset++] = (byte)(48UL + (num7 = num5 * 8389UL >> 23));
						num5 -= num7 * 1000UL;
						IL_02B4:
						buffer[offset++] = (byte)(48UL + (num7 = num5 * 5243UL >> 19));
						num5 -= num7 * 100UL;
						IL_02DC:
						buffer[offset++] = (byte)(48UL + (num7 = num5 * 6554UL >> 16));
						num5 -= num7 * 10UL;
						IL_0304:
						buffer[offset++] = (byte)(48UL + num5);
					}
					buffer[offset++] = (byte)(48UL + (num7 = num4 * 8389UL >> 23));
					num4 -= num7 * 1000UL;
					IL_033C:
					buffer[offset++] = (byte)(48UL + (num7 = num4 * 5243UL >> 19));
					num4 -= num7 * 100UL;
					IL_0361:
					buffer[offset++] = (byte)(48UL + (num7 = num4 * 6554UL >> 16));
					num4 -= num7 * 10UL;
					IL_0386:
					buffer[offset++] = (byte)(48UL + num4);
				}
				buffer[offset++] = (byte)(48UL + (num7 = num3 * 8389UL >> 23));
				num3 -= num7 * 1000UL;
				IL_03BD:
				buffer[offset++] = (byte)(48UL + (num7 = num3 * 5243UL >> 19));
				num3 -= num7 * 100UL;
				IL_03E2:
				buffer[offset++] = (byte)(48UL + (num7 = num3 * 6554UL >> 16));
				num3 -= num7 * 10UL;
				IL_0407:
				buffer[offset++] = (byte)(48UL + num3);
			}
			buffer[offset++] = (byte)(48UL + (num7 = num2 * 8389UL >> 23));
			num2 -= num7 * 1000UL;
			IL_043E:
			buffer[offset++] = (byte)(48UL + (num7 = num2 * 5243UL >> 19));
			num2 -= num7 * 100UL;
			IL_0463:
			buffer[offset++] = (byte)(48UL + (num7 = num2 * 6554UL >> 16));
			num2 -= num7 * 10UL;
			IL_0488:
			buffer[offset++] = (byte)(48UL + num2);
			return offset - num;
		}

		public static int WriteSByte(ref byte[] buffer, int offset, sbyte value)
		{
			return NumberConverter.WriteInt64(ref buffer, offset, (long)value);
		}

		public static int WriteInt16(ref byte[] buffer, int offset, short value)
		{
			return NumberConverter.WriteInt64(ref buffer, offset, (long)value);
		}

		public static int WriteInt32(ref byte[] buffer, int offset, int value)
		{
			return NumberConverter.WriteInt64(ref buffer, offset, (long)value);
		}

		public static int WriteInt64(ref byte[] buffer, int offset, long value)
		{
			int num = offset;
			long num2 = value;
			if (value < 0L)
			{
				if (value == -9223372036854775808L)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 20);
					buffer[offset++] = 45;
					buffer[offset++] = 57;
					buffer[offset++] = 50;
					buffer[offset++] = 50;
					buffer[offset++] = 51;
					buffer[offset++] = 51;
					buffer[offset++] = 55;
					buffer[offset++] = 50;
					buffer[offset++] = 48;
					buffer[offset++] = 51;
					buffer[offset++] = 54;
					buffer[offset++] = 56;
					buffer[offset++] = 53;
					buffer[offset++] = 52;
					buffer[offset++] = 55;
					buffer[offset++] = 55;
					buffer[offset++] = 53;
					buffer[offset++] = 56;
					buffer[offset++] = 48;
					buffer[offset++] = 56;
					return offset - num;
				}
				BinaryUtil.EnsureCapacity(ref buffer, offset, 1);
				buffer[offset++] = 45;
				num2 = -value;
			}
			long num7;
			if (num2 < 10000L)
			{
				if (num2 < 10L)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 1);
					goto IL_059E;
				}
				if (num2 < 100L)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 2);
					goto IL_0579;
				}
				if (num2 < 1000L)
				{
					BinaryUtil.EnsureCapacity(ref buffer, offset, 3);
					goto IL_0554;
				}
				BinaryUtil.EnsureCapacity(ref buffer, offset, 4);
			}
			else
			{
				long num3 = num2 / 10000L;
				num2 -= num3 * 10000L;
				if (num3 < 10000L)
				{
					if (num3 < 10L)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 5);
						goto IL_051D;
					}
					if (num3 < 100L)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 6);
						goto IL_04F8;
					}
					if (num3 < 1000L)
					{
						BinaryUtil.EnsureCapacity(ref buffer, offset, 7);
						goto IL_04D3;
					}
					BinaryUtil.EnsureCapacity(ref buffer, offset, 8);
				}
				else
				{
					long num4 = num3 / 10000L;
					num3 -= num4 * 10000L;
					if (num4 < 10000L)
					{
						if (num4 < 10L)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 9);
							goto IL_049C;
						}
						if (num4 < 100L)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 10);
							goto IL_0477;
						}
						if (num4 < 1000L)
						{
							BinaryUtil.EnsureCapacity(ref buffer, offset, 11);
							goto IL_0452;
						}
						BinaryUtil.EnsureCapacity(ref buffer, offset, 12);
					}
					else
					{
						long num5 = num4 / 10000L;
						num4 -= num5 * 10000L;
						if (num5 < 10000L)
						{
							if (num5 < 10L)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 13);
								goto IL_041A;
							}
							if (num5 < 100L)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 14);
								goto IL_03F2;
							}
							if (num5 < 1000L)
							{
								BinaryUtil.EnsureCapacity(ref buffer, offset, 15);
								goto IL_03CA;
							}
							BinaryUtil.EnsureCapacity(ref buffer, offset, 16);
						}
						else
						{
							long num6 = num5 / 10000L;
							num5 -= num6 * 10000L;
							if (num6 < 10000L)
							{
								if (num6 < 10L)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 17);
									goto IL_038F;
								}
								if (num6 < 100L)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 18);
									goto IL_0367;
								}
								if (num6 < 1000L)
								{
									BinaryUtil.EnsureCapacity(ref buffer, offset, 19);
									goto IL_033F;
								}
								BinaryUtil.EnsureCapacity(ref buffer, offset, 20);
							}
							buffer[offset++] = (byte)(48L + (num7 = num6 * 8389L >> 23));
							num6 -= num7 * 1000L;
							IL_033F:
							buffer[offset++] = (byte)(48L + (num7 = num6 * 5243L >> 19));
							num6 -= num7 * 100L;
							IL_0367:
							buffer[offset++] = (byte)(48L + (num7 = num6 * 6554L >> 16));
							num6 -= num7 * 10L;
							IL_038F:
							buffer[offset++] = (byte)(48L + num6);
						}
						buffer[offset++] = (byte)(48L + (num7 = num5 * 8389L >> 23));
						num5 -= num7 * 1000L;
						IL_03CA:
						buffer[offset++] = (byte)(48L + (num7 = num5 * 5243L >> 19));
						num5 -= num7 * 100L;
						IL_03F2:
						buffer[offset++] = (byte)(48L + (num7 = num5 * 6554L >> 16));
						num5 -= num7 * 10L;
						IL_041A:
						buffer[offset++] = (byte)(48L + num5);
					}
					buffer[offset++] = (byte)(48L + (num7 = num4 * 8389L >> 23));
					num4 -= num7 * 1000L;
					IL_0452:
					buffer[offset++] = (byte)(48L + (num7 = num4 * 5243L >> 19));
					num4 -= num7 * 100L;
					IL_0477:
					buffer[offset++] = (byte)(48L + (num7 = num4 * 6554L >> 16));
					num4 -= num7 * 10L;
					IL_049C:
					buffer[offset++] = (byte)(48L + num4);
				}
				buffer[offset++] = (byte)(48L + (num7 = num3 * 8389L >> 23));
				num3 -= num7 * 1000L;
				IL_04D3:
				buffer[offset++] = (byte)(48L + (num7 = num3 * 5243L >> 19));
				num3 -= num7 * 100L;
				IL_04F8:
				buffer[offset++] = (byte)(48L + (num7 = num3 * 6554L >> 16));
				num3 -= num7 * 10L;
				IL_051D:
				buffer[offset++] = (byte)(48L + num3);
			}
			buffer[offset++] = (byte)(48L + (num7 = num2 * 8389L >> 23));
			num2 -= num7 * 1000L;
			IL_0554:
			buffer[offset++] = (byte)(48L + (num7 = num2 * 5243L >> 19));
			num2 -= num7 * 100L;
			IL_0579:
			buffer[offset++] = (byte)(48L + (num7 = num2 * 6554L >> 16));
			num2 -= num7 * 10L;
			IL_059E:
			buffer[offset++] = (byte)(48L + num2);
			return offset - num;
		}

		public static int WriteSingle(ref byte[] bytes, int offset, float value)
		{
			return DoubleToStringConverter.GetBytes(ref bytes, offset, value);
		}

		public static int WriteDouble(ref byte[] bytes, int offset, double value)
		{
			return DoubleToStringConverter.GetBytes(ref bytes, offset, value);
		}

		public static bool ReadBoolean(byte[] bytes, int offset, out int readCount)
		{
			if (bytes[offset] == 116)
			{
				if (bytes[offset + 1] == 114 && bytes[offset + 2] == 117 && bytes[offset + 3] == 101)
				{
					readCount = 4;
					return true;
				}
				throw new InvalidOperationException("value is not boolean(true).");
			}
			else
			{
				if (bytes[offset] != 102)
				{
					throw new InvalidOperationException("value is not boolean.");
				}
				if (bytes[offset + 1] == 97 && bytes[offset + 2] == 108 && bytes[offset + 3] == 115 && bytes[offset + 4] == 101)
				{
					readCount = 5;
					return false;
				}
				throw new InvalidOperationException("value is not boolean(false).");
			}
		}
	}
}
