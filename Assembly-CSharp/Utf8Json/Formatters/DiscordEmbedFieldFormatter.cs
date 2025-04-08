using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class DiscordEmbedFieldFormatter : IJsonFormatter<DiscordEmbedField>, IJsonFormatter
	{
		public DiscordEmbedFieldFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("name"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("value"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("inline"),
					2
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("name"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("value"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("inline")
			};
		}

		public void Serialize(ref JsonWriter writer, DiscordEmbedField value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.name);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.value);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteBoolean(value.inline);
			writer.WriteEndObject();
		}

		public DiscordEmbedField Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			bool flag = false;
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
						flag = reader.ReadBoolean();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new DiscordEmbedField(text, text2, flag);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
