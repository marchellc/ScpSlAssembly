using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NewsListFormatter : IJsonFormatter<NewsList>, IJsonFormatter
	{
		public NewsListFormatter()
		{
			this.____keyMapping = new AutomataDictionary
			{
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("appid"),
					0
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("newsitems"),
					1
				},
				{
					JsonWriter.GetEncodedPropertyNameWithoutQuotation("count"),
					2
				}
			};
			this.____stringByteKeys = new byte[][]
			{
				JsonWriter.GetEncodedPropertyNameWithBeginObject("appid"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("newsitems"),
				JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator("count")
			};
		}

		public void Serialize(ref JsonWriter writer, NewsList value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			writer.WriteInt32(value.appid);
			writer.WriteRaw(this.____stringByteKeys[1]);
			formatterResolver.GetFormatterWithVerify<NewsListItem[]>().Serialize(ref writer, value.newsitems, formatterResolver);
			writer.WriteRaw(this.____stringByteKeys[2]);
			writer.WriteInt32(value.count);
			writer.WriteEndObject();
		}

		public NewsList Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			int num = 0;
			NewsListItem[] array = null;
			int num2 = 0;
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
						num = reader.ReadInt32();
						break;
					case 1:
						array = formatterResolver.GetFormatterWithVerify<NewsListItem[]>().Deserialize(ref reader, formatterResolver);
						break;
					case 2:
						num2 = reader.ReadInt32();
						break;
					default:
						reader.ReadNextBlock();
						break;
					}
				}
			}
			return new NewsList(num, array, num2);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
