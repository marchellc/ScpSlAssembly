using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class RenewResponseFormatter : IJsonFormatter<RenewResponse>, IJsonFormatter
	{
		public RenewResponseFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("success"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("error"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("id"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("nonce"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("country"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("flags"),
					5
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("expiration"),
					6
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("preauth"),
					7
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("globalBan"),
					8
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("lifetime"),
					9
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("success"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("error"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("id"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("nonce"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("country"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("flags"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("expiration"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("preauth"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("globalBan"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("lifetime")
			};
		}

		public void Serialize(ref JsonWriter writer, RenewResponse value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteBoolean(value.success);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.error);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.id);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteString(value.nonce);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.country);
			writer.WriteRaw(this.____stringByteKeys[5]);
			writer.WriteByte(value.flags);
			writer.WriteRaw(this.____stringByteKeys[6]);
			writer.WriteInt64(value.expiration);
			writer.WriteRaw(this.____stringByteKeys[7]);
			writer.WriteString(value.preauth);
			writer.WriteRaw(this.____stringByteKeys[8]);
			writer.WriteString(value.globalBan);
			writer.WriteRaw(this.____stringByteKeys[9]);
			writer.WriteUInt16(value.lifetime);
			writer.WriteEndObject();
		}

		public RenewResponse Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			bool flag = false;
			string text = null;
			string text2 = null;
			string text3 = null;
			string text4 = null;
			byte b = 0;
			long num = 0L;
			string text5 = null;
			string text6 = null;
			ushort num2 = 0;
			int num3 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num3))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num4;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num4))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num4)
					{
					case 0:
						flag = reader.ReadBoolean();
						break;
					case 1:
						text = reader.ReadString();
						break;
					case 2:
						text2 = reader.ReadString();
						break;
					case 3:
						text3 = reader.ReadString();
						break;
					case 4:
						text4 = reader.ReadString();
						break;
					case 5:
						b = reader.ReadByte();
						break;
					case 6:
						num = reader.ReadInt64();
						break;
					case 7:
						text5 = reader.ReadString();
						break;
					case 8:
						text6 = reader.ReadString();
						break;
					case 9:
						num2 = reader.ReadUInt16();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new RenewResponse(flag, text, text2, text3, text4, b, num, text5, text6, num2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
