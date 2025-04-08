using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class CreditsListMemberFormatter : IJsonFormatter<CreditsListMember>, IJsonFormatter
	{
		public CreditsListMemberFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("name"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("title"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("color"),
					2
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("name"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("title"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("color")
			};
		}

		public void Serialize(ref JsonWriter writer, CreditsListMember value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.name);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.title);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.color);
			writer.WriteEndObject();
		}

		public CreditsListMember Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
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
			return new CreditsListMember(text, text2, text3);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
