using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class DiscordWebhookFormatter : IJsonFormatter<DiscordWebhook>, IJsonFormatter
	{
		public DiscordWebhookFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("content"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("username"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("avatar_url"),
					2
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("tts"),
					3
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("embeds"),
					4
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("content"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("username"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("avatar_url"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("tts"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("embeds")
			};
		}

		public void Serialize(ref JsonWriter writer, DiscordWebhook value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteString(value.content);
			writer.WriteRaw(this.____stringByteKeys[1]);
			writer.WriteString(value.username);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteString(value.avatar_url);
			writer.WriteRaw(this.____stringByteKeys[3]);
			writer.WriteBoolean(value.tts);
			writer.WriteRaw(this.____stringByteKeys[4]);
			formatterResolver.GetFormatterWithVerify<DiscordEmbed[]>().Serialize(ref writer, value.embeds, formatterResolver);
			writer.WriteEndObject();
		}

		public DiscordWebhook Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			string text = null;
			string text2 = null;
			string text3 = null;
			bool flag = false;
			DiscordEmbed[] array = null;
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
						flag = reader.ReadBoolean();
						break;
					case 4:
						array = formatterResolver.GetFormatterWithVerify<DiscordEmbed[]>().Deserialize(ref reader, formatterResolver);
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new DiscordWebhook(text, text2, text3, flag, array);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
