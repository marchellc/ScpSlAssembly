using System;

namespace Utf8Json.Internal.DoubleConversion
{
	internal static class StringToDouble
	{
		private static byte[] GetCopyBuffer()
		{
			if (StringToDouble.copyBuffer == null)
			{
				StringToDouble.copyBuffer = new byte[780];
			}
			return StringToDouble.copyBuffer;
		}

		private static Vector TrimLeadingZeros(Vector buffer)
		{
			for (int i = 0; i < buffer.length(); i++)
			{
				if (buffer[i] != 48)
				{
					return buffer.SubVector(i, buffer.length());
				}
			}
			return new Vector(buffer.bytes, buffer.start, 0);
		}

		private static Vector TrimTrailingZeros(Vector buffer)
		{
			for (int i = buffer.length() - 1; i >= 0; i--)
			{
				if (buffer[i] != 48)
				{
					return buffer.SubVector(0, i + 1);
				}
			}
			return new Vector(buffer.bytes, buffer.start, 0);
		}

		private static void CutToMaxSignificantDigits(Vector buffer, int exponent, byte[] significant_buffer, out int significant_exponent)
		{
			for (int i = 0; i < 779; i++)
			{
				significant_buffer[i] = buffer[i];
			}
			significant_buffer[779] = 49;
			significant_exponent = exponent + (buffer.length() - 780);
		}

		private static void TrimAndCut(Vector buffer, int exponent, byte[] buffer_copy_space, int space_size, out Vector trimmed, out int updated_exponent)
		{
			Vector vector = StringToDouble.TrimLeadingZeros(buffer);
			Vector vector2 = StringToDouble.TrimTrailingZeros(vector);
			exponent += vector.length() - vector2.length();
			if (vector2.length() > 780)
			{
				StringToDouble.CutToMaxSignificantDigits(vector2, exponent, buffer_copy_space, out updated_exponent);
				trimmed = new Vector(buffer_copy_space, 0, 780);
				return;
			}
			trimmed = vector2;
			updated_exponent = exponent;
		}

		private static ulong ReadUint64(Vector buffer, out int number_of_read_digits)
		{
			ulong num = 0UL;
			int num2 = 0;
			while (num2 < buffer.length() && num <= 1844674407370955160UL)
			{
				int num3 = (int)(buffer[num2++] - 48);
				num = 10UL * num + (ulong)((long)num3);
			}
			number_of_read_digits = num2;
			return num;
		}

		private static void ReadDiyFp(Vector buffer, out DiyFp result, out int remaining_decimals)
		{
			int num2;
			ulong num = StringToDouble.ReadUint64(buffer, out num2);
			if (buffer.length() == num2)
			{
				result = new DiyFp(num, 0);
				remaining_decimals = 0;
				return;
			}
			if (buffer[num2] >= 53)
			{
				num += 1UL;
			}
			int num3 = 0;
			result = new DiyFp(num, num3);
			remaining_decimals = buffer.length() - num2;
		}

		private static bool DoubleStrtod(Vector trimmed, int exponent, out double result)
		{
			if (trimmed.length() <= 15)
			{
				if (exponent < 0 && -exponent < StringToDouble.kExactPowersOfTenSize)
				{
					int num;
					result = StringToDouble.ReadUint64(trimmed, out num);
					result /= StringToDouble.exact_powers_of_ten[-exponent];
					return true;
				}
				if (0 <= exponent && exponent < StringToDouble.kExactPowersOfTenSize)
				{
					int num;
					result = StringToDouble.ReadUint64(trimmed, out num);
					result *= StringToDouble.exact_powers_of_ten[exponent];
					return true;
				}
				int num2 = 15 - trimmed.length();
				if (0 <= exponent && exponent - num2 < StringToDouble.kExactPowersOfTenSize)
				{
					int num;
					result = StringToDouble.ReadUint64(trimmed, out num);
					result *= StringToDouble.exact_powers_of_ten[num2];
					result *= StringToDouble.exact_powers_of_ten[exponent - num2];
					return true;
				}
			}
			result = 0.0;
			return false;
		}

		private static DiyFp AdjustmentPowerOfTen(int exponent)
		{
			switch (exponent)
			{
			case 1:
				return new DiyFp(11529215046068469760UL, -60);
			case 2:
				return new DiyFp(14411518807585587200UL, -57);
			case 3:
				return new DiyFp(18014398509481984000UL, -54);
			case 4:
				return new DiyFp(11258999068426240000UL, -50);
			case 5:
				return new DiyFp(14073748835532800000UL, -47);
			case 6:
				return new DiyFp(17592186044416000000UL, -44);
			case 7:
				return new DiyFp(10995116277760000000UL, -40);
			default:
				throw new Exception("unreached code.");
			}
		}

		private static bool DiyFpStrtod(Vector buffer, int exponent, out double result)
		{
			DiyFp diyFp;
			int num;
			StringToDouble.ReadDiyFp(buffer, out diyFp, out num);
			exponent += num;
			ulong num2 = (ulong)((num == 0) ? 0L : 4L);
			int num3 = diyFp.e;
			diyFp.Normalize();
			num2 <<= num3 - diyFp.e;
			if (exponent < -348)
			{
				result = 0.0;
				return true;
			}
			DiyFp diyFp2;
			int num4;
			PowersOfTenCache.GetCachedPowerForDecimalExponent(exponent, out diyFp2, out num4);
			if (num4 != exponent)
			{
				int num5 = exponent - num4;
				DiyFp diyFp3 = StringToDouble.AdjustmentPowerOfTen(num5);
				diyFp.Multiply(ref diyFp3);
				if (19 - buffer.length() < num5)
				{
					num2 += 4UL;
				}
			}
			diyFp.Multiply(ref diyFp2);
			int num6 = 4;
			int num7 = ((num2 == 0UL) ? 0 : 1);
			int num8 = 4;
			num2 += (ulong)((long)(num6 + num7 + num8));
			num3 = diyFp.e;
			diyFp.Normalize();
			num2 <<= num3 - diyFp.e;
			int num9 = Double.SignificandSizeForOrderOfMagnitude(64 + diyFp.e);
			int num10 = 64 - num9;
			if (num10 + 3 >= 64)
			{
				int num11 = num10 + 3 - 64 + 1;
				diyFp.f >>= num11;
				diyFp.e += num11;
				num2 = (num2 >> num11) + 1UL + 8UL;
				num10 -= num11;
			}
			long num12 = 1L;
			ulong num13 = (ulong)((num12 << num10) - 1L);
			ulong num14 = diyFp.f & num13;
			ulong num15 = (ulong)((ulong)num12 << num10 - 1);
			num14 *= 8UL;
			num15 *= 8UL;
			DiyFp diyFp4 = new DiyFp(diyFp.f >> num10, diyFp.e + num10);
			if (num14 >= num15 + num2)
			{
				diyFp4.f += 1UL;
			}
			result = new Double(diyFp4).value();
			return num15 - num2 >= num14 || num14 >= num15 + num2;
		}

		private static bool ComputeGuess(Vector trimmed, int exponent, out double guess)
		{
			if (trimmed.length() == 0)
			{
				guess = 0.0;
				return true;
			}
			if (exponent + trimmed.length() - 1 >= 309)
			{
				guess = Double.Infinity();
				return true;
			}
			if (exponent + trimmed.length() <= -324)
			{
				guess = 0.0;
				return true;
			}
			return StringToDouble.DoubleStrtod(trimmed, exponent, out guess) || StringToDouble.DiyFpStrtod(trimmed, exponent, out guess) || guess == Double.Infinity();
		}

		public static double? Strtod(Vector buffer, int exponent)
		{
			byte[] array = StringToDouble.GetCopyBuffer();
			Vector vector;
			int num;
			StringToDouble.TrimAndCut(buffer, exponent, array, 780, out vector, out num);
			exponent = num;
			double num2;
			if (StringToDouble.ComputeGuess(vector, exponent, out num2))
			{
				return new double?(num2);
			}
			return null;
		}

		public static float? Strtof(Vector buffer, int exponent)
		{
			byte[] array = StringToDouble.GetCopyBuffer();
			Vector vector;
			int num;
			StringToDouble.TrimAndCut(buffer, exponent, array, 780, out vector, out num);
			exponent = num;
			double num2;
			bool flag = StringToDouble.ComputeGuess(vector, exponent, out num2);
			float num3 = (float)num2;
			if ((double)num3 == num2)
			{
				return new float?(num3);
			}
			double num4 = new Double(num2).NextDouble();
			float num5 = (float)new Double(num2).PreviousDouble();
			float num6 = (float)num4;
			float num7;
			if (flag)
			{
				num7 = num6;
			}
			else
			{
				num7 = (float)new Double(num4).NextDouble();
			}
			if (num5 == num7)
			{
				return new float?(num3);
			}
			return null;
		}

		[ThreadStatic]
		private static byte[] copyBuffer;

		private const int kMaxExactDoubleIntegerDecimalDigits = 15;

		private const int kMaxUint64DecimalDigits = 19;

		private const int kMaxDecimalPower = 309;

		private const int kMinDecimalPower = -324;

		private const ulong kMaxUint64 = 18446744073709551615UL;

		private static readonly double[] exact_powers_of_ten = new double[]
		{
			1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
			10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1000000000000000.0, 10000000000000000.0, 1E+17, 1E+18, 1E+19,
			1E+20, 1E+21, 1E+22
		};

		private static readonly int kExactPowersOfTenSize = StringToDouble.exact_powers_of_ten.Length;

		private const int kMaxSignificantDecimalDigits = 780;
	}
}
