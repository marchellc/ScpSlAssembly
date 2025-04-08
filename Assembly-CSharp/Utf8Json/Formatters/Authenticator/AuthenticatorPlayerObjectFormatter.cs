using System;
using Authenticator;
using Utf8Json.Internal;

namespace Utf8Json.Formatters.Authenticator
{
	public sealed class AuthenticatorPlayerObjectFormatter : IJsonFormatter<AuthenticatorPlayerObject>, IJsonFormatter
	{
		public AuthenticatorPlayerObjectFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Id"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Ip"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("RequestIp"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("Asn"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("AuthSerial"),
					4
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("VacSession"),
					5
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("Id"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Ip"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("RequestIp"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("Asn"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("AuthSerial"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("VacSession")
			};
		}

		public void Serialize(ref JsonWriter writer, AuthenticatorPlayerObject value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.Id);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.Ip);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.RequestIp);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteString(value.Asn);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.AuthSerial);
			writer.WriteRaw(this.____stringByteKeys[5]);
			writer.WriteString(value.VacSession);
			writer.WriteEndObject();
		}

		public AuthenticatorPlayerObject Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			string text3 = null;
			string text4 = null;
			string text5 = null;
			string text6 = null;
			int num = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num2))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num2)
					{
					case 0:
						text = reader.ReadString();
						break;
					case 1:
						text2 = reader.ReadString();
						break;
					case 2:
						text3 = reader.ReadString();
						break;
					case 3:
						text4 = reader.ReadString();
						break;
					case 4:
						text5 = reader.ReadString();
						break;
					case 5:
						text6 = reader.ReadString();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new AuthenticatorPlayerObject(text, text2, text3, text4, text5, text6);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
