using System;
using System.Collections.Generic;
using System.Linq;
using Utf8Json.Formatters.Internal;

namespace Utf8Json.Formatters
{
	public sealed class InterfaceGroupingFormatter<TKey, TElement> : IJsonFormatter<IGrouping<TKey, TElement>>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, IGrouping<TKey, TElement> value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteRaw(CollectionFormatterHelper.groupingName[0]);
			formatterResolver.GetFormatterWithVerify<TKey>().Serialize(ref writer, value.Key, formatterResolver);
			writer.WriteRaw(CollectionFormatterHelper.groupingName[1]);
			formatterResolver.GetFormatterWithVerify<IEnumerable<TElement>>().Serialize(ref writer, value.AsEnumerable<TElement>(), formatterResolver);
			writer.WriteEndObject();
		}

		public IGrouping<TKey, TElement> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			TKey tkey = default(TKey);
			IEnumerable<TElement> enumerable = null;
			reader.ReadIsBeginObjectWithVerify();
			int num = 0;
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				CollectionFormatterHelper.groupingAutomata.TryGetValueSafe(arraySegment, out num2);
				if (num2 != 0)
				{
					if (num2 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						enumerable = formatterResolver.GetFormatterWithVerify<IEnumerable<TElement>>().Deserialize(ref reader, formatterResolver);
					}
				}
				else
				{
					tkey = formatterResolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, formatterResolver);
				}
			}
			return new Grouping<TKey, TElement>(tkey, enumerable);
		}
	}
}
