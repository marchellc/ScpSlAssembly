using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ISO8601DateTimeOffsetFormatter : IJsonFormatter<DateTimeOffset>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, DateTimeOffset value, IJsonFormatterResolver formatterResolver)
		{
			int year = value.Year;
			int month = value.Month;
			int day = value.Day;
			int hour = value.Hour;
			int minute = value.Minute;
			int second = value.Second;
			long num = value.Ticks % 10000000L;
			writer.EnsureCapacity(21 + ((num == 0L) ? 0 : 8) + 6);
			writer.WriteRawUnsafe(34);
			if (year < 10)
			{
				writer.WriteRawUnsafe(48);
				writer.WriteRawUnsafe(48);
				writer.WriteRawUnsafe(48);
			}
			else if (year < 100)
			{
				writer.WriteRawUnsafe(48);
				writer.WriteRawUnsafe(48);
			}
			else if (year < 1000)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(year);
			writer.WriteRawUnsafe(45);
			if (month < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(month);
			writer.WriteRawUnsafe(45);
			if (day < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(day);
			writer.WriteRawUnsafe(84);
			if (hour < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(hour);
			writer.WriteRawUnsafe(58);
			if (minute < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(minute);
			writer.WriteRawUnsafe(58);
			if (second < 10)
			{
				writer.WriteRawUnsafe(48);
			}
			writer.WriteInt32(second);
			if (num != 0L)
			{
				writer.WriteRawUnsafe(46);
				if (num < 10L)
				{
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
				}
				else if (num < 100L)
				{
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
				}
				else if (num < 1000L)
				{
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
				}
				else if (num < 10000L)
				{
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
				}
				else if (num < 100000L)
				{
					writer.WriteRawUnsafe(48);
					writer.WriteRawUnsafe(48);
				}
				else if (num < 1000000L)
				{
					writer.WriteRawUnsafe(48);
				}
				writer.WriteInt64(num);
			}
			TimeSpan timeSpan = value.Offset;
			bool flag = timeSpan < TimeSpan.Zero;
			if (flag)
			{
				timeSpan = timeSpan.Negate();
			}
			int hours = timeSpan.Hours;
			int minutes = timeSpan.Minutes;
			writer.WriteRawUnsafe(flag ? 45 : 43);
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
			writer.WriteRawUnsafe(34);
		}

		public DateTimeOffset Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
			byte[] array = arraySegment.Array;
			int num = arraySegment.Offset;
			int count = arraySegment.Count;
			int num2 = arraySegment.Offset + arraySegment.Count;
			if (count == 4)
			{
				return new DateTimeOffset((int)(array[num++] - 48) * 1000 + (int)((array[num++] - 48) * 100) + (int)((array[num++] - 48) * 10) + (int)(array[num++] - 48), 1, 1, 0, 0, 0, TimeSpan.Zero);
			}
			if (count == 7)
			{
				int num3 = (int)(array[num++] - 48) * 1000 + (int)((array[num++] - 48) * 100) + (int)((array[num++] - 48) * 10) + (int)(array[num++] - 48);
				if (array[num++] == 45)
				{
					int num4 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
					return new DateTimeOffset(num3, num4, 1, 0, 0, 0, TimeSpan.Zero);
				}
			}
			else if (count == 10)
			{
				int num5 = (int)(array[num++] - 48) * 1000 + (int)((array[num++] - 48) * 100) + (int)((array[num++] - 48) * 10) + (int)(array[num++] - 48);
				if (array[num++] == 45)
				{
					int num6 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
					if (array[num++] == 45)
					{
						int num7 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
						return new DateTimeOffset(num5, num6, num7, 0, 0, 0, TimeSpan.Zero);
					}
				}
			}
			else if (array.Length >= 19)
			{
				int num8 = (int)(array[num++] - 48) * 1000 + (int)((array[num++] - 48) * 100) + (int)((array[num++] - 48) * 10) + (int)(array[num++] - 48);
				if (array[num++] == 45)
				{
					int num9 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
					if (array[num++] == 45)
					{
						int num10 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
						if (array[num++] == 84)
						{
							int num11 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
							if (array[num++] == 58)
							{
								int num12 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
								if (array[num++] == 58)
								{
									int num13 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
									int num14 = 0;
									if (num < num2 && array[num] == 46)
									{
										num++;
										if (num < num2 && NumberConverter.IsNumber(array[num]))
										{
											num14 += (int)(array[num] - 48) * 1000000;
											num++;
											if (num < num2 && NumberConverter.IsNumber(array[num]))
											{
												num14 += (int)(array[num] - 48) * 100000;
												num++;
												if (num < num2 && NumberConverter.IsNumber(array[num]))
												{
													num14 += (int)(array[num] - 48) * 10000;
													num++;
													if (num < num2 && NumberConverter.IsNumber(array[num]))
													{
														num14 += (int)(array[num] - 48) * 1000;
														num++;
														if (num < num2 && NumberConverter.IsNumber(array[num]))
														{
															num14 += (int)((array[num] - 48) * 100);
															num++;
															if (num < num2 && NumberConverter.IsNumber(array[num]))
															{
																num14 += (int)((array[num] - 48) * 10);
																num++;
																if (num < num2 && NumberConverter.IsNumber(array[num]))
																{
																	num14 += (int)(array[num] - 48);
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
									if ((num >= num2 || array[num] != 45) && array[num] != 43)
									{
										return new DateTimeOffset(num8, num9, num10, num11, num12, num13, TimeSpan.Zero).AddTicks((long)num14);
									}
									if (num + 5 < num2)
									{
										bool flag = array[num++] == 45;
										int num15 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
										num++;
										int num16 = (int)((array[num++] - 48) * 10 + (array[num++] - 48));
										TimeSpan timeSpan = new TimeSpan(num15, num16, 0);
										if (flag)
										{
											timeSpan = timeSpan.Negate();
										}
										return new DateTimeOffset(num8, num9, num10, num11, num12, num13, timeSpan).AddTicks((long)num14);
									}
								}
							}
						}
					}
				}
			}
			throw new InvalidOperationException("invalid datetime format. value:" + StringEncoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count));
		}

		public static readonly IJsonFormatter<DateTimeOffset> Default = new ISO8601DateTimeOffsetFormatter();
	}
}
