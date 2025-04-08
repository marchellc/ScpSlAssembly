using System;
using System.Reflection;

namespace Utf8Json.Internal
{
	public static class AutomataKeyGen
	{
		public unsafe static ulong GetKey(ref byte* p, ref int rest)
		{
			ulong num;
			int num2;
			if (rest >= 8)
			{
				num = (ulong)(*p);
				num2 = 8;
			}
			else
			{
				switch (rest)
				{
				case 1:
					num = (ulong)(*p);
					num2 = 1;
					break;
				case 2:
					num = (ulong)(*p);
					num2 = 2;
					break;
				case 3:
				{
					ulong num3 = (ulong)(*p);
					ushort num4 = *(p + 1);
					num = num3 | ((ulong)num4 << 8);
					num2 = 3;
					break;
				}
				case 4:
					num = (ulong)(*p);
					num2 = 4;
					break;
				case 5:
				{
					ulong num5 = (ulong)(*p);
					uint num6 = *(p + 1);
					num = num5 | ((ulong)num6 << 8);
					num2 = 5;
					break;
				}
				case 6:
				{
					ulong num7 = (ulong)(*p);
					ulong num8 = (ulong)(*(p + 2));
					num = num7 | (num8 << 16);
					num2 = 6;
					break;
				}
				case 7:
				{
					ulong num9 = (ulong)(*p);
					ushort num10 = *(p + 1);
					uint num11 = *(p + 3);
					num = num9 | ((ulong)num10 << 8) | ((ulong)num11 << 24);
					num2 = 7;
					break;
				}
				default:
					throw new InvalidOperationException("Not Supported Length");
				}
			}
			p += (IntPtr)num2;
			rest -= num2;
			return num;
		}

		public static ulong GetKeySafe(byte[] bytes, ref int offset, ref int rest)
		{
			ulong num;
			int num2;
			if (BitConverter.IsLittleEndian)
			{
				if (rest >= 8)
				{
					num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 24) | ((ulong)bytes[offset + 4] << 32) | ((ulong)bytes[offset + 5] << 40) | ((ulong)bytes[offset + 6] << 48) | ((ulong)bytes[offset + 7] << 56);
					num2 = 8;
				}
				else
				{
					switch (rest)
					{
					case 1:
						num = (ulong)bytes[offset];
						num2 = 1;
						break;
					case 2:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8);
						num2 = 2;
						break;
					case 3:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16);
						num2 = 3;
						break;
					case 4:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 24);
						num2 = 4;
						break;
					case 5:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 24) | ((ulong)bytes[offset + 4] << 32);
						num2 = 5;
						break;
					case 6:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 24) | ((ulong)bytes[offset + 4] << 32) | ((ulong)bytes[offset + 5] << 40);
						num2 = 6;
						break;
					case 7:
						num = (ulong)bytes[offset] | ((ulong)bytes[offset + 1] << 8) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 24) | ((ulong)bytes[offset + 4] << 32) | ((ulong)bytes[offset + 5] << 40) | ((ulong)bytes[offset + 6] << 48);
						num2 = 7;
						break;
					default:
						throw new InvalidOperationException("Not Supported Length");
					}
				}
				offset += num2;
				rest -= num2;
				return num;
			}
			if (rest >= 8)
			{
				num = ((ulong)bytes[offset] << 56) | ((ulong)bytes[offset + 1] << 48) | ((ulong)bytes[offset + 2] << 40) | ((ulong)bytes[offset + 3] << 32) | ((ulong)bytes[offset + 4] << 24) | ((ulong)bytes[offset + 5] << 16) | ((ulong)bytes[offset + 6] << 8) | (ulong)bytes[offset + 7];
				num2 = 8;
			}
			else
			{
				switch (rest)
				{
				case 1:
					num = (ulong)bytes[offset];
					num2 = 1;
					break;
				case 2:
					num = ((ulong)bytes[offset] << 8) | (ulong)bytes[offset + 1];
					num2 = 2;
					break;
				case 3:
					num = ((ulong)bytes[offset] << 16) | ((ulong)bytes[offset + 1] << 8) | (ulong)bytes[offset + 2];
					num2 = 3;
					break;
				case 4:
					num = ((ulong)bytes[offset] << 24) | ((ulong)bytes[offset + 1] << 16) | ((ulong)bytes[offset + 2] << 8) | (ulong)bytes[offset + 3];
					num2 = 4;
					break;
				case 5:
					num = ((ulong)bytes[offset] << 32) | ((ulong)bytes[offset + 1] << 24) | ((ulong)bytes[offset + 2] << 16) | ((ulong)bytes[offset + 3] << 8) | (ulong)bytes[offset + 4];
					num2 = 5;
					break;
				case 6:
					num = ((ulong)bytes[offset] << 40) | ((ulong)bytes[offset + 1] << 32) | ((ulong)bytes[offset + 2] << 24) | ((ulong)bytes[offset + 3] << 16) | ((ulong)bytes[offset + 4] << 8) | (ulong)bytes[offset + 5];
					num2 = 6;
					break;
				case 7:
					num = ((ulong)bytes[offset] << 48) | ((ulong)bytes[offset + 1] << 40) | ((ulong)bytes[offset + 2] << 32) | ((ulong)bytes[offset + 3] << 24) | ((ulong)bytes[offset + 4] << 16) | ((ulong)bytes[offset + 5] << 8) | (ulong)bytes[offset + 6];
					num2 = 7;
					break;
				default:
					throw new InvalidOperationException("Not Supported Length");
				}
			}
			offset += num2;
			rest -= num2;
			return num;
		}

		public static readonly MethodInfo GetKeyMethod = typeof(AutomataKeyGen).GetRuntimeMethod("GetKey", new Type[]
		{
			typeof(byte*).MakeByRefType(),
			typeof(int).MakeByRefType()
		});
	}
}
