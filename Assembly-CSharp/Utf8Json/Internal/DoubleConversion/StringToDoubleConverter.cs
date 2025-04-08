using System;
using System.Text;

namespace Utf8Json.Internal.DoubleConversion
{
	internal static class StringToDoubleConverter
	{
		private static byte[] GetBuffer()
		{
			if (StringToDoubleConverter.kBuffer == null)
			{
				StringToDoubleConverter.kBuffer = new byte[782];
			}
			return StringToDoubleConverter.kBuffer;
		}

		private static byte[] GetFallbackBuffer()
		{
			if (StringToDoubleConverter.fallbackBuffer == null)
			{
				StringToDoubleConverter.fallbackBuffer = new byte[99];
			}
			return StringToDoubleConverter.fallbackBuffer;
		}

		public static double ToDouble(byte[] buffer, int offset, out int readCount)
		{
			return StringToDoubleConverter.StringToIeee(new Iterator(buffer, offset), buffer.Length - offset, true, out readCount);
		}

		public static float ToSingle(byte[] buffer, int offset, out int readCount)
		{
			return (float)StringToDoubleConverter.StringToIeee(new Iterator(buffer, offset), buffer.Length - offset, false, out readCount);
		}

		private static bool isWhitespace(int x)
		{
			if (x < 128)
			{
				for (int i = 0; i < StringToDoubleConverter.kWhitespaceTable7Length; i++)
				{
					if ((int)StringToDoubleConverter.kWhitespaceTable7[i] == x)
					{
						return true;
					}
				}
			}
			else
			{
				for (int j = 0; j < StringToDoubleConverter.kWhitespaceTable16Length; j++)
				{
					if ((int)StringToDoubleConverter.kWhitespaceTable16[j] == x)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool AdvanceToNonspace(ref Iterator current, Iterator end)
		{
			while (current != end)
			{
				if (!StringToDoubleConverter.isWhitespace((int)current.Value))
				{
					return true;
				}
				current = Iterator.op_Increment(current);
			}
			return false;
		}

		private static bool ConsumeSubString(ref Iterator current, Iterator end, byte[] substring)
		{
			for (int i = 1; i < substring.Length; i++)
			{
				current = Iterator.op_Increment(current);
				if (current == end || current != substring[i])
				{
					return false;
				}
			}
			current = Iterator.op_Increment(current);
			return true;
		}

		private static bool ConsumeFirstCharacter(ref Iterator iter, byte[] str, int offset)
		{
			return iter.Value == str[offset];
		}

		private static double SignedZero(bool sign)
		{
			if (!sign)
			{
				return 0.0;
			}
			return -0.0;
		}

		private static double StringToIeee(Iterator input, int length, bool read_as_double, out int processed_characters_count)
		{
			Iterator iterator = input;
			Iterator iterator2 = input + length;
			processed_characters_count = 0;
			bool flag = true;
			bool flag2 = false;
			bool flag3 = true;
			bool flag4 = true;
			if (length == 0)
			{
				return 0.0;
			}
			if (flag2 || flag3)
			{
				if (!StringToDoubleConverter.AdvanceToNonspace(ref iterator, iterator2))
				{
					processed_characters_count = iterator - input;
					return 0.0;
				}
				if (!flag2 && input != iterator)
				{
					return double.NaN;
				}
			}
			byte[] buffer = StringToDoubleConverter.GetBuffer();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			bool flag5 = false;
			bool flag6 = false;
			if (iterator == '+' || iterator == '-')
			{
				flag6 = iterator == '-';
				iterator = Iterator.op_Increment(iterator);
				Iterator iterator3 = iterator;
				if (!StringToDoubleConverter.AdvanceToNonspace(ref iterator3, iterator2))
				{
					return double.NaN;
				}
				if (!flag4 && iterator != iterator3)
				{
					return double.NaN;
				}
				iterator = iterator3;
			}
			if (StringToDoubleConverter.infinity_symbol_ != null && StringToDoubleConverter.ConsumeFirstCharacter(ref iterator, StringToDoubleConverter.infinity_symbol_, 0))
			{
				if (!StringToDoubleConverter.ConsumeSubString(ref iterator, iterator2, StringToDoubleConverter.infinity_symbol_))
				{
					return double.NaN;
				}
				if (!flag3 && !flag && iterator != iterator2)
				{
					return double.NaN;
				}
				if (!flag && StringToDoubleConverter.AdvanceToNonspace(ref iterator, iterator2))
				{
					return double.NaN;
				}
				processed_characters_count = iterator - input;
				if (!flag6)
				{
					return double.PositiveInfinity;
				}
				return double.NegativeInfinity;
			}
			else if (StringToDoubleConverter.nan_symbol_ != null && StringToDoubleConverter.ConsumeFirstCharacter(ref iterator, StringToDoubleConverter.nan_symbol_, 0))
			{
				if (!StringToDoubleConverter.ConsumeSubString(ref iterator, iterator2, StringToDoubleConverter.nan_symbol_))
				{
					return double.NaN;
				}
				if (!flag3 && !flag && iterator != iterator2)
				{
					return double.NaN;
				}
				if (!flag && StringToDoubleConverter.AdvanceToNonspace(ref iterator, iterator2))
				{
					return double.NaN;
				}
				processed_characters_count = iterator - input;
				if (!flag6)
				{
					return double.NaN;
				}
				return double.NaN;
			}
			else
			{
				bool flag7 = false;
				if (iterator == '0')
				{
					iterator = Iterator.op_Increment(iterator);
					if (iterator == iterator2)
					{
						processed_characters_count = iterator - input;
						return StringToDoubleConverter.SignedZero(flag6);
					}
					flag7 = true;
					while (iterator == '0')
					{
						iterator = Iterator.op_Increment(iterator);
						if (iterator == iterator2)
						{
							processed_characters_count = iterator - input;
							return StringToDoubleConverter.SignedZero(flag6);
						}
					}
				}
				bool flag8 = flag7 && false;
				while (iterator >= '0' && iterator <= '9')
				{
					if (num3 < 772)
					{
						buffer[num++] = iterator.Value;
						num3++;
					}
					else
					{
						num4++;
						flag5 = flag5 || iterator != '0';
					}
					iterator = Iterator.op_Increment(iterator);
					if (iterator == iterator2)
					{
						IL_0506:
						num2 += num4;
						if (flag5)
						{
							buffer[num++] = 49;
							num2--;
						}
						buffer[num] = 0;
						double? num5;
						if (read_as_double)
						{
							num5 = StringToDouble.Strtod(new Vector(buffer, 0, num), num2);
						}
						else
						{
							float? num6 = StringToDouble.Strtof(new Vector(buffer, 0, num), num2);
							num5 = ((num6 != null) ? new double?((double)num6.GetValueOrDefault()) : null);
						}
						if (num5 == null)
						{
							processed_characters_count = iterator - input;
							byte[] array = StringToDoubleConverter.GetFallbackBuffer();
							BinaryUtil.EnsureCapacity(ref StringToDoubleConverter.fallbackBuffer, 0, processed_characters_count);
							int num7 = 0;
							while (input != iterator)
							{
								array[num7++] = input.Value;
								input = Iterator.op_Increment(input);
							}
							return double.Parse(Encoding.UTF8.GetString(array, 0, num7));
						}
						processed_characters_count = iterator - input;
						if (!flag6)
						{
							return num5.Value;
						}
						return -num5.Value;
					}
				}
				if (num3 == 0)
				{
					flag8 = false;
				}
				if (iterator == '.')
				{
					if (flag8 && !flag)
					{
						return double.NaN;
					}
					if (flag8)
					{
						goto IL_0506;
					}
					iterator = Iterator.op_Increment(iterator);
					if (iterator == iterator2)
					{
						if (num3 == 0 && !flag7)
						{
							return double.NaN;
						}
						goto IL_0506;
					}
					else
					{
						if (num3 == 0)
						{
							while (iterator == '0')
							{
								iterator = Iterator.op_Increment(iterator);
								if (iterator == iterator2)
								{
									processed_characters_count = iterator - input;
									return StringToDoubleConverter.SignedZero(flag6);
								}
								num2--;
							}
						}
						while (iterator >= '0' && iterator <= '9')
						{
							if (num3 < 772)
							{
								buffer[num++] = iterator.Value;
								num3++;
								num2--;
							}
							else
							{
								flag5 = flag5 || iterator != '0';
							}
							iterator = Iterator.op_Increment(iterator);
							if (iterator == iterator2)
							{
								goto IL_0506;
							}
						}
					}
				}
				if (!flag7 && num2 == 0 && num3 == 0)
				{
					return double.NaN;
				}
				if (iterator == 'e' || iterator == 'E')
				{
					if (flag8 && !flag)
					{
						return double.NaN;
					}
					if (flag8)
					{
						goto IL_0506;
					}
					iterator = Iterator.op_Increment(iterator);
					if (iterator == iterator2)
					{
						if (!flag)
						{
							return double.NaN;
						}
						goto IL_0506;
					}
					else
					{
						byte b = 43;
						if (iterator == '+' || iterator == '-')
						{
							b = iterator.Value;
							iterator = Iterator.op_Increment(iterator);
							if (iterator == iterator2)
							{
								if (!flag)
								{
									return double.NaN;
								}
								goto IL_0506;
							}
						}
						if (iterator == iterator2 || iterator < '0' || iterator > '9')
						{
							if (!flag)
							{
								return double.NaN;
							}
							goto IL_0506;
						}
						else
						{
							int num8 = 0;
							do
							{
								int num9 = (int)(iterator.Value - 48);
								if (num8 >= 107374182 && (num8 != 107374182 || num9 > 3))
								{
									num8 = 1073741823;
								}
								else
								{
									num8 = num8 * 10 + num9;
								}
								iterator = Iterator.op_Increment(iterator);
							}
							while (iterator != iterator2 && iterator >= '0' && iterator <= '9');
							num2 += ((b == 45) ? (-num8) : num8);
						}
					}
				}
				if (!flag3 && !flag && iterator != iterator2)
				{
					return double.NaN;
				}
				if (!flag && StringToDoubleConverter.AdvanceToNonspace(ref iterator, iterator2))
				{
					return double.NaN;
				}
				if (flag3)
				{
					StringToDoubleConverter.AdvanceToNonspace(ref iterator, iterator2);
					goto IL_0506;
				}
				goto IL_0506;
			}
		}

		[ThreadStatic]
		private static byte[] kBuffer;

		[ThreadStatic]
		private static byte[] fallbackBuffer;

		private const StringToDoubleConverter.Flags flags_ = (StringToDoubleConverter.Flags)52;

		private const double empty_string_value_ = 0.0;

		private const double junk_string_value_ = double.NaN;

		private const int kMaxSignificantDigits = 772;

		private const int kBufferSize = 782;

		private static readonly byte[] infinity_symbol_ = StringEncoding.UTF8.GetBytes(double.PositiveInfinity.ToString());

		private static readonly byte[] nan_symbol_ = StringEncoding.UTF8.GetBytes(double.NaN.ToString());

		private static readonly byte[] kWhitespaceTable7 = new byte[] { 32, 13, 10, 9, 11, 12 };

		private static readonly int kWhitespaceTable7Length = StringToDoubleConverter.kWhitespaceTable7.Length;

		private static readonly ushort[] kWhitespaceTable16 = new ushort[]
		{
			160, 8232, 8233, 5760, 6158, 8192, 8193, 8194, 8195, 8196,
			8197, 8198, 8199, 8200, 8201, 8202, 8239, 8287, 12288, 65279
		};

		private static readonly int kWhitespaceTable16Length = StringToDoubleConverter.kWhitespaceTable16.Length;

		private enum Flags
		{
			NO_FLAGS,
			ALLOW_HEX,
			ALLOW_OCTALS,
			ALLOW_TRAILING_JUNK = 4,
			ALLOW_LEADING_SPACES = 8,
			ALLOW_TRAILING_SPACES = 16,
			ALLOW_SPACES_AFTER_SIGN = 32,
			ALLOW_CASE_INSENSIBILITY = 64
		}
	}
}
