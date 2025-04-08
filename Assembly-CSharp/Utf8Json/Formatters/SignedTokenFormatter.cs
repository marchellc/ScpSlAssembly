using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class SignedTokenFormatter : IJsonFormatter<SignedToken>, IJsonFormatter
	{
		public SignedTokenFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("token"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("signature"),
					1
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("token"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("signature")
			};
		}

		public void Serialize(ref JsonWriter writer, SignedToken value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.token);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.signature);
			writer.WriteEndObject();
		}

		public SignedToken Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			string text = null;
			string text2 = null;
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
				else if (num2 != 0)
				{
					if (num2 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						text2 = reader.ReadString();
					}
				}
				else
				{
					text = reader.ReadString();
				}
			}
			return new SignedToken(text, text2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
