using System;
using System.Collections.Generic;
using Utf8Json.Formatters.Internal;

namespace Utf8Json.Formatters
{
	public sealed class KeyValuePairFormatter<TKey, TValue> : IJsonFormatter<KeyValuePair<TKey, TValue>>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, KeyValuePair<TKey, TValue> value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteRaw(StandardClassLibraryFormatterHelper.keyValuePairName[0]);
			formatterResolver.GetFormatterWithVerify<TKey>().Serialize(ref writer, value.Key, formatterResolver);
			writer.WriteRaw(StandardClassLibraryFormatterHelper.keyValuePairName[1]);
			formatterResolver.GetFormatterWithVerify<TValue>().Serialize(ref writer, value.Value, formatterResolver);
			writer.WriteEndObject();
		}

		public KeyValuePair<TKey, TValue> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				throw new InvalidOperationException("Data is Nil, KeyValuePair can not be null.");
			}
			TKey tkey = default(TKey);
			TValue tvalue = default(TValue);
			reader.ReadIsBeginObjectWithVerify();
			int num = 0;
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				ArraySegment<byte> arraySegment = reader.ReadPropertyNameSegmentRaw();
				int num2;
				StandardClassLibraryFormatterHelper.keyValuePairAutomata.TryGetValueSafe(arraySegment, out num2);
				if (num2 != 0)
				{
					if (num2 != 1)
					{
						reader.ReadNextBlock();
					}
					else
					{
						tvalue = formatterResolver.GetFormatterWithVerify<TValue>().Deserialize(ref reader, formatterResolver);
					}
				}
				else
				{
					tkey = formatterResolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, formatterResolver);
				}
			}
			return new KeyValuePair<TKey, TValue>(tkey, tvalue);
		}
	}
}
