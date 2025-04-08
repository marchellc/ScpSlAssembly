using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class DiscordEmbedFormatter : IJsonFormatter<DiscordEmbed>, IJsonFormatter
	{
		public DiscordEmbedFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("title"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("type"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("description"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("color"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("fields"),
					4
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("title"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("type"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("description"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("color"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("fields")
			};
		}

		public void Serialize(ref JsonWriter writer, DiscordEmbed value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.title);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.type);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.description);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteInt32(value.color);
			writer.WriteRaw(this.____stringByteKeys[4]);
			formatterResolver.GetFormatterWithVerify<DiscordEmbedField[]>().Serialize(ref writer, value.fields, formatterResolver);
			writer.WriteEndObject();
		}

		public DiscordEmbed Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			string text3 = null;
			int num = 0;
			DiscordEmbedField[] array = null;
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
						text2 = reader.ReadString();
						break;
					case 2:
						text3 = reader.ReadString();
						break;
					case 3:
						num = reader.ReadInt32();
						break;
					case 4:
						array = formatterResolver.GetFormatterWithVerify<DiscordEmbedField[]>().Deserialize(ref reader, formatterResolver);
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new DiscordEmbed(text, text2, text3, num, array);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
