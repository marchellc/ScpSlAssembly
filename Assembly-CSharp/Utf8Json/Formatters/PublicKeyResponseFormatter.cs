using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class PublicKeyResponseFormatter : IJsonFormatter<PublicKeyResponse>, IJsonFormatter
	{
		public PublicKeyResponseFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("key"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("signature"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("credits"),
					2
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("key"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("signature"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("credits")
			};
		}

		public void Serialize(ref JsonWriter writer, PublicKeyResponse value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.key);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.signature);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.credits);
			writer.WriteEndObject();
		}

		public PublicKeyResponse Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			string text3 = null;
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
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new PublicKeyResponse(text, text2, text3);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
