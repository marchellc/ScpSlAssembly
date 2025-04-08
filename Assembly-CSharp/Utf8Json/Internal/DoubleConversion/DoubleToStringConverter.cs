using System;
using System.Globalization;

namespace Utf8Json.Internal.DoubleConversion
{
	internal static class DoubleToStringConverter
	{
		private static byte[] GetDecimalRepBuffer(int size)
		{
			if (DoubleToStringConverter.decimalRepBuffer == null)
			{
				DoubleToStringConverter.decimalRepBuffer = new byte[size];
			}
			return DoubleToStringConverter.decimalRepBuffer;
		}

		private static byte[] GetExponentialRepBuffer(int size)
		{
			if (DoubleToStringConverter.exponentialRepBuffer == null)
			{
				DoubleToStringConverter.exponentialRepBuffer = new byte[size];
			}
			return DoubleToStringConverter.exponentialRepBuffer;
		}

		private static byte[] GetToStringBuffer()
		{
			if (DoubleToStringConverter.toStringBuffer == null)
			{
				DoubleToStringConverter.toStringBuffer = new byte[24];
			}
			return DoubleToStringConverter.toStringBuffer;
		}

		public static int GetBytes(ref byte[] buffer, int offset, float value)
		{
			StringBuilder stringBuilder = new StringBuilder(buffer, offset);
			if (!DoubleToStringConverter.ToShortestIeeeNumber((double)value, ref stringBuilder, DoubleToStringConverter.DtoaMode.SHORTEST_SINGLE))
			{
				throw new InvalidOperationException("not support float value:" + value.ToString());
			}
			buffer = stringBuilder.buffer;
			return stringBuilder.offset - offset;
		}

		public static int GetBytes(ref byte[] buffer, int offset, double value)
		{
			StringBuilder stringBuilder = new StringBuilder(buffer, offset);
			if (!DoubleToStringConverter.ToShortestIeeeNumber(value, ref stringBuilder, DoubleToStringConverter.DtoaMode.SHORTEST))
			{
				throw new InvalidOperationException("not support double value:" + value.ToString());
			}
			buffer = stringBuilder.buffer;
			return stringBuilder.offset - offset;
		}

		private static bool RoundWeed(byte[] buffer, int length, ulong distance_too_high_w, ulong unsafe_interval, ulong rest, ulong ten_kappa, ulong unit)
		{
			ulong num = distance_too_high_w - unit;
			ulong num2 = distance_too_high_w + unit;
			while (rest < num && unsafe_interval - rest >= ten_kappa && (rest + ten_kappa < num || num - rest >= rest + ten_kappa - num))
			{
				int num3 = length - 1;
				buffer[num3] -= 1;
				rest += ten_kappa;
			}
			return (rest >= num2 || unsafe_interval - rest < ten_kappa || (rest + ten_kappa >= num2 && num2 - rest <= rest + ten_kappa - num2)) && 2UL * unit <= rest && rest <= unsafe_interval - 4UL * unit;
		}

		private static void BiggestPowerTen(uint number, int number_bits, out uint power, out int exponent_plus_one)
		{
			int num = (number_bits + 1) * 1233 >> 12;
			num++;
			if (number < DoubleToStringConverter.kSmallPowersOfTen[num])
			{
				num--;
			}
			power = DoubleToStringConverter.kSmallPowersOfTen[num];
			exponent_plus_one = num;
		}

		private static bool DigitGen(DiyFp low, DiyFp w, DiyFp high, byte[] buffer, out int length, out int kappa)
		{
			ulong num = 1UL;
			DiyFp diyFp = new DiyFp(low.f - num, low.e);
			DiyFp diyFp2 = new DiyFp(high.f + num, high.e);
			DiyFp diyFp3 = DiyFp.Minus(ref diyFp2, ref diyFp);
			DiyFp diyFp4 = new DiyFp(1UL << -w.e, w.e);
			uint num2 = (uint)(diyFp2.f >> -diyFp4.e);
			ulong num3 = diyFp2.f & (diyFp4.f - 1UL);
			uint num4;
			int num5;
			DoubleToStringConverter.BiggestPowerTen(num2, 64 - -diyFp4.e, out num4, out num5);
			kappa = num5;
			length = 0;
			while (kappa > 0)
			{
				int num6 = (int)(num2 / num4);
				buffer[length] = (byte)(48 + num6);
				length++;
				num2 %= num4;
				kappa--;
				ulong num7 = ((ulong)num2 << -diyFp4.e) + num3;
				if (num7 < diyFp3.f)
				{
					return DoubleToStringConverter.RoundWeed(buffer, length, DiyFp.Minus(ref diyFp2, ref w).f, diyFp3.f, num7, (ulong)num4 << -diyFp4.e, num);
				}
				num4 /= 10U;
			}
			do
			{
				num3 *= 10UL;
				num *= 10UL;
				diyFp3.f *= 10UL;
				int num8 = (int)(num3 >> -diyFp4.e);
				buffer[length] = (byte)(48 + num8);
				length++;
				num3 &= diyFp4.f - 1UL;
				kappa--;
			}
			while (num3 >= diyFp3.f);
			return DoubleToStringConverter.RoundWeed(buffer, length, DiyFp.Minus(ref diyFp2, ref w).f * num, diyFp3.f, num3, diyFp4.f, num);
		}

		private static bool Grisu3(double v, DoubleToStringConverter.FastDtoaMode mode, byte[] buffer, out int length, out int decimal_exponent)
		{
			DiyFp diyFp = new Double(v).AsNormalizedDiyFp();
			DiyFp diyFp2;
			DiyFp diyFp3;
			if (mode == DoubleToStringConverter.FastDtoaMode.FAST_DTOA_SHORTEST)
			{
				new Double(v).NormalizedBoundaries(out diyFp2, out diyFp3);
			}
			else
			{
				if (mode != DoubleToStringConverter.FastDtoaMode.FAST_DTOA_SHORTEST_SINGLE)
				{
					throw new Exception("Invalid Mode.");
				}
				new Single((float)v).NormalizedBoundaries(out diyFp2, out diyFp3);
			}
			int num = -60 - (diyFp.e + 64);
			int num2 = -32 - (diyFp.e + 64);
			DiyFp diyFp4;
			int num3;
			PowersOfTenCache.GetCachedPowerForBinaryExponentRange(num, num2, out diyFp4, out num3);
			DiyFp diyFp5 = DiyFp.Times(ref diyFp, ref diyFp4);
			DiyFp diyFp6 = DiyFp.Times(ref diyFp2, ref diyFp4);
			DiyFp diyFp7 = DiyFp.Times(ref diyFp3, ref diyFp4);
			int num4;
			bool flag = DoubleToStringConverter.DigitGen(diyFp6, diyFp5, diyFp7, buffer, out length, out num4);
			decimal_exponent = -num3 + num4;
			return flag;
		}

		private static bool FastDtoa(double v, DoubleToStringConverter.FastDtoaMode mode, byte[] buffer, out int length, out int decimal_point)
		{
			int num = 0;
			if (mode <= DoubleToStringConverter.FastDtoaMode.FAST_DTOA_SHORTEST_SINGLE)
			{
				bool flag = DoubleToStringConverter.Grisu3(v, mode, buffer, out length, out num);
				if (flag)
				{
					decimal_point = length + num;
				}
				else
				{
					decimal_point = -1;
				}
				return flag;
			}
			throw new Exception("unreachable code.");
		}

		private static bool HandleSpecialValues(double value, ref StringBuilder result_builder)
		{
			Double @double = new Double(value);
			if (@double.IsInfinite())
			{
				if (DoubleToStringConverter.infinity_symbol_ == null)
				{
					return false;
				}
				if (value < 0.0)
				{
					result_builder.AddCharacter(45);
				}
				result_builder.AddString(DoubleToStringConverter.infinity_symbol_);
				return true;
			}
			else
			{
				if (!@double.IsNan())
				{
					return false;
				}
				if (DoubleToStringConverter.nan_symbol_ == null)
				{
					return false;
				}
				result_builder.AddString(DoubleToStringConverter.nan_symbol_);
				return true;
			}
		}

		private static bool ToShortestIeeeNumber(double value, ref StringBuilder result_builder, DoubleToStringConverter.DtoaMode mode)
		{
			if (new Double(value).IsSpecial())
			{
				return DoubleToStringConverter.HandleSpecialValues(value, ref result_builder);
			}
			byte[] array = DoubleToStringConverter.GetDecimalRepBuffer(18);
			bool flag;
			int num;
			int num2;
			if (!DoubleToStringConverter.DoubleToAscii(value, mode, 0, array, out flag, out num, out num2))
			{
				string text = value.ToString("G17", CultureInfo.InvariantCulture);
				result_builder.AddStringSlow(text);
				return true;
			}
			bool flag2 = (DoubleToStringConverter.flags_ & DoubleToStringConverter.Flags.UNIQUE_ZERO) > DoubleToStringConverter.Flags.NO_FLAGS;
			if (flag && (value != 0.0 || !flag2))
			{
				result_builder.AddCharacter(45);
			}
			int num3 = num2 - 1;
			if (DoubleToStringConverter.decimal_in_shortest_low_ <= num3 && num3 < DoubleToStringConverter.decimal_in_shortest_high_)
			{
				DoubleToStringConverter.CreateDecimalRepresentation(array, num, num2, Math.Max(0, num - num2), ref result_builder);
			}
			else
			{
				DoubleToStringConverter.CreateExponentialRepresentation(array, num, num3, ref result_builder);
			}
			return true;
		}

		private static void CreateDecimalRepresentation(byte[] decimal_digits, int length, int decimal_point, int digits_after_point, ref StringBuilder result_builder)
		{
			if (decimal_point <= 0)
			{
				result_builder.AddCharacter(48);
				if (digits_after_point > 0)
				{
					result_builder.AddCharacter(46);
					result_builder.AddPadding(48, -decimal_point);
					result_builder.AddSubstring(decimal_digits, length);
					int num = digits_after_point - -decimal_point - length;
					result_builder.AddPadding(48, num);
				}
			}
			else if (decimal_point >= length)
			{
				result_builder.AddSubstring(decimal_digits, length);
				result_builder.AddPadding(48, decimal_point - length);
				if (digits_after_point > 0)
				{
					result_builder.AddCharacter(46);
					result_builder.AddPadding(48, digits_after_point);
				}
			}
			else
			{
				result_builder.AddSubstring(decimal_digits, decimal_point);
				result_builder.AddCharacter(46);
				result_builder.AddSubstring(decimal_digits, decimal_point, length - decimal_point);
				int num2 = digits_after_point - (length - decimal_point);
				result_builder.AddPadding(48, num2);
			}
			if (digits_after_point == 0)
			{
				if ((DoubleToStringConverter.flags_ & DoubleToStringConverter.Flags.EMIT_TRAILING_DECIMAL_POINT) != DoubleToStringConverter.Flags.NO_FLAGS)
				{
					result_builder.AddCharacter(46);
				}
				if ((DoubleToStringConverter.flags_ & DoubleToStringConverter.Flags.EMIT_TRAILING_ZERO_AFTER_POINT) != DoubleToStringConverter.Flags.NO_FLAGS)
				{
					result_builder.AddCharacter(48);
				}
			}
		}

		private static void CreateExponentialRepresentation(byte[] decimal_digits, int length, int exponent, ref StringBuilder result_builder)
		{
			result_builder.AddCharacter(decimal_digits[0]);
			if (length != 1)
			{
				result_builder.AddCharacter(46);
				result_builder.AddSubstring(decimal_digits, 1, length - 1);
			}
			result_builder.AddCharacter((byte)DoubleToStringConverter.exponent_character_);
			if (exponent < 0)
			{
				result_builder.AddCharacter(45);
				exponent = -exponent;
			}
			else if ((DoubleToStringConverter.flags_ & DoubleToStringConverter.Flags.EMIT_POSITIVE_EXPONENT_SIGN) != DoubleToStringConverter.Flags.NO_FLAGS)
			{
				result_builder.AddCharacter(43);
			}
			if (exponent == 0)
			{
				result_builder.AddCharacter(48);
				return;
			}
			byte[] array = DoubleToStringConverter.GetExponentialRepBuffer(6);
			array[5] = 0;
			int num = 5;
			while (exponent > 0)
			{
				array[--num] = (byte)(48 + exponent % 10);
				exponent /= 10;
			}
			result_builder.AddSubstring(array, num, 5 - num);
		}

		private static bool DoubleToAscii(double v, DoubleToStringConverter.DtoaMode mode, int requested_digits, byte[] vector, out bool sign, out int length, out int point)
		{
			if (new Double(v).Sign() < 0)
			{
				sign = true;
				v = -v;
			}
			else
			{
				sign = false;
			}
			if (v == 0.0)
			{
				vector[0] = 48;
				length = 1;
				point = 1;
				return true;
			}
			bool flag;
			if (mode != DoubleToStringConverter.DtoaMode.SHORTEST)
			{
				if (mode != DoubleToStringConverter.DtoaMode.SHORTEST_SINGLE)
				{
					throw new Exception("Unreachable code.");
				}
				flag = DoubleToStringConverter.FastDtoa(v, DoubleToStringConverter.FastDtoaMode.FAST_DTOA_SHORTEST_SINGLE, vector, out length, out point);
			}
			else
			{
				flag = DoubleToStringConverter.FastDtoa(v, DoubleToStringConverter.FastDtoaMode.FAST_DTOA_SHORTEST, vector, out length, out point);
			}
			return flag;
		}

		[ThreadStatic]
		private static byte[] decimalRepBuffer;

		[ThreadStatic]
		private static byte[] exponentialRepBuffer;

		[ThreadStatic]
		private static byte[] toStringBuffer;

		private static readonly byte[] infinity_symbol_ = StringEncoding.UTF8.GetBytes(double.PositiveInfinity.ToString());

		private static readonly byte[] nan_symbol_ = StringEncoding.UTF8.GetBytes(double.NaN.ToString());

		private static readonly DoubleToStringConverter.Flags flags_ = (DoubleToStringConverter.Flags)9;

		private static readonly char exponent_character_ = 'E';

		private static readonly int decimal_in_shortest_low_ = -4;

		private static readonly int decimal_in_shortest_high_ = 15;

		private const int kBase10MaximalLength = 17;

		private const int kFastDtoaMaximalLength = 17;

		private const int kFastDtoaMaximalSingleLength = 9;

		private const int kMinimalTargetExponent = -60;

		private const int kMaximalTargetExponent = -32;

		private static readonly uint[] kSmallPowersOfTen = new uint[]
		{
			0U, 1U, 10U, 100U, 1000U, 10000U, 100000U, 1000000U, 10000000U, 100000000U,
			1000000000U
		};

		private enum FastDtoaMode
		{
			FAST_DTOA_SHORTEST,
			FAST_DTOA_SHORTEST_SINGLE
		}

		private enum DtoaMode
		{
			SHORTEST,
			SHORTEST_SINGLE
		}

		private enum Flags
		{
			NO_FLAGS,
			EMIT_POSITIVE_EXPONENT_SIGN,
			EMIT_TRAILING_DECIMAL_POINT,
			EMIT_TRAILING_ZERO_AFTER_POINT = 4,
			UNIQUE_ZERO = 8
		}
	}
}
