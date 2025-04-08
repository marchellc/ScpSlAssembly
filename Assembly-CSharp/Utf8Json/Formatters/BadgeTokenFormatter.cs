using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class BadgeTokenFormatter : IJsonFormatter<BadgeToken>, IJsonFormatter
	{
		public BadgeTokenFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("BadgeText"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("BadgeColor"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("BadgeType"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Staff"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("RemoteAdmin"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Management"),
					5
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("OverwatchMode"),
					6
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("GlobalBanning"),
					7
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("RaPermissions"),
					8
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("UserId"),
					9
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Nickname"),
					10
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("IssuanceTime"),
					11
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("ExpirationTime"),
					12
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Usage"),
					13
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("IssuedBy"),
					14
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Serial"),
					15
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("TestSignature"),
					16
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("TokenVersion"),
					17
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("BadgeText"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("BadgeColor"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("BadgeType"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Staff"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("RemoteAdmin"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Management"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("OverwatchMode"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("GlobalBanning"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("RaPermissions"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("UserId"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Nickname"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("IssuanceTime"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("ExpirationTime"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Usage"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("IssuedBy"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Serial"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("TestSignature"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("TokenVersion")
			};
		}

		public void Serialize(ref JsonWriter writer, BadgeToken value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.BadgeText);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.BadgeColor);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteInt32(value.BadgeType);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteBoolean(value.Staff);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteBoolean(value.RemoteAdmin);
			writer.WriteRaw(this.____stringByteKeys[5]);
			writer.WriteBoolean(value.Management);
			writer.WriteRaw(this.____stringByteKeys[6]);
			writer.WriteBoolean(value.OverwatchMode);
			writer.WriteRaw(this.____stringByteKeys[7]);
			writer.WriteBoolean(value.GlobalBanning);
			writer.WriteRaw(this.____stringByteKeys[8]);
			writer.WriteUInt64(value.RaPermissions);
			writer.WriteRaw(this.____stringByteKeys[9]);
			writer.WriteString(value.UserId);
			writer.WriteRaw(this.____stringByteKeys[10]);
			writer.WriteString(value.Nickname);
			writer.WriteRaw(this.____stringByteKeys[11]);
			formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Serialize(ref writer, value.IssuanceTime, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[12]);
			formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Serialize(ref writer, value.ExpirationTime, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[13]);
			writer.WriteString(value.Usage);
			writer.WriteRaw(this.____stringByteKeys[14]);
			writer.WriteString(value.IssuedBy);
			writer.WriteRaw(this.____stringByteKeys[15]);
			writer.WriteString(value.Serial);
			writer.WriteRaw(this.____stringByteKeys[16]);
			writer.WriteBoolean(value.TestSignature);
			writer.WriteRaw(this.____stringByteKeys[17]);
			writer.WriteInt32(value.TokenVersion);
			writer.WriteEndObject();
		}

		public BadgeToken Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			string text = null;
			string text2 = null;
			int num = 0;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			ulong num2 = 0UL;
			string text3 = null;
			string text4 = null;
			DateTimeOffset dateTimeOffset = default(DateTimeOffset);
			DateTimeOffset dateTimeOffset2 = default(DateTimeOffset);
			string text5 = null;
			string text6 = null;
			string text7 = null;
			bool flag6 = false;
			int num3 = 0;
			int num4 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num4))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num5;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num5))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num5)
					{
					case 0:
						text = reader.ReadString();
						break;
					case 1:
						text2 = reader.ReadString();
						break;
					case 2:
						num = reader.ReadInt32();
						break;
					case 3:
						flag = reader.ReadBoolean();
						break;
					case 4:
						flag2 = reader.ReadBoolean();
						break;
					case 5:
						flag3 = reader.ReadBoolean();
						break;
					case 6:
						flag4 = reader.ReadBoolean();
						break;
					case 7:
						flag5 = reader.ReadBoolean();
						break;
					case 8:
						num2 = reader.ReadUInt64();
						break;
					case 9:
						text3 = reader.ReadString();
						break;
					case 10:
						text4 = reader.ReadString();
						break;
					case 11:
						dateTimeOffset = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
						break;
					case 12:
						dateTimeOffset2 = formatterResolver.GetFormatterWithVerify<DateTimeOffset>().Deserialize(ref reader, formatterResolver);
						break;
					case 13:
						text5 = reader.ReadString();
						break;
					case 14:
						text6 = reader.ReadString();
						break;
					case 15:
						text7 = reader.ReadString();
						break;
					case 16:
						flag6 = reader.ReadBoolean();
						break;
					case 17:
						num3 = reader.ReadInt32();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new BadgeToken(text, text2, num, flag, flag2, flag3, flag4, flag5, num2, text3, text4, dateTimeOffset, dateTimeOffset2, text5, text6, text7, flag6, num3);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
