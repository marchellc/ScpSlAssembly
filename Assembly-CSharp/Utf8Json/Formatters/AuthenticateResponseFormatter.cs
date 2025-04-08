using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class AuthenticateResponseFormatter : IJsonFormatter<AuthenticateResponse>, IJsonFormatter
	{
		public AuthenticateResponseFormatter()
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
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("token"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("id"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("nonce"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("country"),
					5
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("flags"),
					6
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("expiration"),
					7
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("preauth"),
					8
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("globalBan"),
					9
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("lifetime"),
					10
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("NoWatermarking"),
					11
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("success"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("error"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("token"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("id"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("nonce"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("country"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("flags"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("expiration"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("preauth"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("globalBan"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("lifetime"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("NoWatermarking")
			};
		}

		public void Serialize(ref JsonWriter writer, AuthenticateResponse value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteBoolean(value.success);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.error);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.token);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteString(value.id);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.nonce);
			writer.WriteRaw(this.____stringByteKeys[5]);
			writer.WriteString(value.country);
			writer.WriteRaw(this.____stringByteKeys[6]);
			writer.WriteByte(value.flags);
			writer.WriteRaw(this.____stringByteKeys[7]);
			writer.WriteInt64(value.expiration);
			writer.WriteRaw(this.____stringByteKeys[8]);
			writer.WriteString(value.preauth);
			writer.WriteRaw(this.____stringByteKeys[9]);
			writer.WriteString(value.globalBan);
			writer.WriteRaw(this.____stringByteKeys[10]);
			writer.WriteUInt16(value.lifetime);
			writer.WriteRaw(this.____stringByteKeys[11]);
			writer.WriteBoolean(value.NoWatermarking);
			writer.WriteEndObject();
		}

		public AuthenticateResponse Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
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
			string text5 = null;
			byte b = 0;
			long num = 0L;
			string text6 = null;
			string text7 = null;
			ushort num2 = 0;
			bool flag2 = false;
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
						text5 = reader.ReadString();
						break;
					case 6:
						b = reader.ReadByte();
						break;
					case 7:
						num = reader.ReadInt64();
						break;
					case 8:
						text6 = reader.ReadString();
						break;
					case 9:
						text7 = reader.ReadString();
						break;
					case 10:
						num2 = reader.ReadUInt16();
						break;
					case 11:
						flag2 = reader.ReadBoolean();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new AuthenticateResponse(flag, text, text2, text3, text4, text5, b, num, text6, text7, num2, flag2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
