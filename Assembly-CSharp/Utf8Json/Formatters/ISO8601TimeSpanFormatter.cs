using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ISO8601TimeSpanFormatter : IJsonFormatter<TimeSpan>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, TimeSpan value, IJsonFormatterResolver formatterResolver)
		{
			if (value == TimeSpan.MinValue)
			{
				writer.WriteRaw(ISO8601TimeSpanFormatter.minValue);
				return;
			}
			bool flag = value < TimeSpan.Zero;
			if (flag)
			{
				value = value.Negate();
			}
			int days = value.Days;
			int hours = value.Hours;
			int minutes = value.Minutes;
			int seconds = value.Seconds;
			long num = value.Ticks % 10000000L;
			writer.EnsureCapacity(19 + ((num == 0L) ? 0 : 8) + 6);
			writer.WriteRawUnsafe(34);
			if (flag)
			{
				writer.WriteRawUnsafe(45);
			}
			if (days != 0)
			{
				writer.WriteInt32(days);
				writer.WriteRawUnsafe(46);
			}
			if (hours < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(hours);
			writer.WriteRawUnsafe(58);
			if (minutes < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(minutes);
			writer.WriteRawUnsafe(58);
			if (seconds < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(seconds);
			if (num != 0L)
			{
				writer.WriteRawUnsafe(46);
				writer.WriteInt64(num);
			}
			writer.WriteRawUnsafe(34);
		}

		public TimeSpan Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
			byte[] array = arraySegment.Array;
			int num = arraySegment.Offset;
			int count = arraySegment.Count;
			int num2 = arraySegment.Offset + arraySegment.Count;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			for (int i = num; i < arraySegment.Count; i++)
			{
				if (array[i] == 46)
				{
					if (flag3)
					{
						break;
					}
					flag2 = true;
				}
				else if (array[i] == 58)
				{
					if (flag2)
					{
						flag = true;
					}
					flag3 = true;
				}
			}
			bool flag4 = false;
			if (array[num] == 45)
			{
				flag4 = true;
				num++;
			}
			int num3 = 0;
			if (flag)
			{
				byte[] array2 = BufferPool.Default.Rent();
				try
				{
					while (array[num] != 46)
					{
						array2[num3++] = array[num];
						num++;
					}
					num3 = new JsonReader(array2).ReadInt32();
					num++;
				}
				finally
				{
					BufferPool.Default.Return(array2);
				}
			}
			int num4 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
			if (array[num++] == 58)
			{
				int num5 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
				if (array[num++] == 58)
				{
					int num6 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
					int num7 = 0;
					if (num < num2 && array[num] == 46)
					{
						num++;
						if (num < num2 && NumberConverter.IsNumber(array[num]))
						{
							num7 += (int)(array[num] - 48) * 1000000;
							num++;
							if (num < num2 && NumberConverter.IsNumber(array[num]))
							{
								num7 += (int)(array[num] - 48) * 100000;
								num++;
								if (num < num2 && NumberConverter.IsNumber(array[num]))
								{
									num7 += (int)(array[num] - 48) * 10000;
									num++;
									if (num < num2 && NumberConverter.IsNumber(array[num]))
									{
										num7 += (int)(array[num] - 48) * 1000;
										num++;
										if (num < num2 && NumberConverter.IsNumber(array[num]))
										{
											num7 += (int)((array[num] - 48) * 100);
											num++;
											if (num < num2 && NumberConverter.IsNumber(array[num]))
											{
												num7 += (int)((array[num] - 48) * 10);
												num++;
												if (num < num2 && NumberConverter.IsNumber(array[num]))
												{
													num7 += (int)(array[num] - 48);
													num++;
													while (num < num2 && NumberConverter.IsNumber(array[num]))
													{
														num++;
													}
												}
											}
										}
									}
								}
							}
						}
					}
					TimeSpan timeSpan = new TimeSpan(num3, num4, num5, num6);
					TimeSpan timeSpan2 = TimeSpan.FromTicks((long)num7);
					if (!flag4)
					{
						return timeSpan.Add(timeSpan2);
					}
					return timeSpan.Negate().Subtract(timeSpan2);
				}
			}
			throw new InvalidOperationException("invalid datetime format. value:" + StringEncoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count));
		}

		public static readonly IJsonFormatter<TimeSpan> Default = new ISO8601TimeSpanFormatter();

		private static byte[] minValue = StringEncoding.UTF8.GetBytes("\"" + TimeSpan.MinValue.ToString() + "\"");
	}
}
