using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ServerListSignedFormatter : IJsonFormatter<ServerListSigned>, IJsonFormatter
	{
		public ServerListSignedFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("payload"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("timestamp"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("signature"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("nonce"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("error"),
					4
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("payload"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("timestamp"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("signature"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("nonce"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("error")
			};
		}

		public void Serialize(ref JsonWriter writer, ServerListSigned value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.payload);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteInt64(value.timestamp);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.signature);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteString(value.nonce);
			writer.WriteRaw(this.____stringByteKeys[4]);
			writer.WriteString(value.error);
			writer.WriteEndObject();
		}

		public ServerListSigned Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			long num = 0L;
			string text2 = null;
			string text3 = null;
			string text4 = null;
			int num2 = 0;
			reader.ReadIsBeginObjectWithVerify();
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num2))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num3;
				if (!this.____keyMapping.TryGetValueSafe(arraySegment, out num3))
				{
					reader.ReadNextBlock();
				}
				else
				{
					switch (num3)
					{
					case 0:
						text = reader.ReadString();
						break;
					case 1:
						num = reader.ReadInt64();
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
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new ServerListSigned(text, num, text2, text3, text4);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
