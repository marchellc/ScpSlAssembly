using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NewsRawFormatter : IJsonFormatter<NewsRaw>, IJsonFormatter
	{
		public NewsRawFormatter()
		{
			this.____keyMapping = new AutomataDictionary { 
			{
				JsonWriter.GetEncodedPropertyNameWithoutQuotation("appnews"),
				0
			} };
			this.____stringByteKeys = new byte[][] { JsonWriter.GetEncodedPropertyNameWithBeginObject("appnews") };
		}

		public void Serialize(ref JsonWriter writer, NewsRaw value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(this.____stringByteKeys[0]);
			formatterResolver.GetFormatterWithVerify<NewsList>().Serialize(ref writer, value.appnews, formatterResolver);
			writer.WriteEndObject();
		}

		public NewsRaw Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			NewsList newsList = default(NewsList);
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
				else if (num2 == 0)
				{
					newsList = formatterResolver.GetFormatterWithVerify<NewsList>().Deserialize(ref reader, formatterResolver);
				}
				else
				{
					reader.ReadNextBlock();
				}
			}
			return new NewsRaw(newsList);
		}

		private readonly AutomataDictionary ____keyMapping;

		private readonly byte[][] ____stringByteKeys;
	}
}
