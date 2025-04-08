using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TEnumerator, TDictionary> : IJsonFormatter<TDictionary>, IJsonFormatter where TEnumerator : IEnumerator<KeyValuePair<TKey, TValue>> where TDictionary : class, IEnumerable<KeyValuePair<TKey, TValue>>
	{
		public void Serialize(ref JsonWriter writer, TDictionary value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			IObjectPropertyNameFormatter<TKey> objectPropertyNameFormatter = formatterResolver.GetFormatterWithVerify<TKey>() as IObjectPropertyNameFormatter<TKey>;
			IJsonFormatter<TValue> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TValue>();
			writer.WriteBeginObject();
			TEnumerator sourceEnumerator = this.GetSourceEnumerator(value);
			try
			{
				if (objectPropertyNameFormatter != null)
				{
					if (sourceEnumerator.MoveNext())
					{
						KeyValuePair<TKey, TValue> keyValuePair = sourceEnumerator.Current;
						objectPropertyNameFormatter.SerializeToPropertyName(ref writer, keyValuePair.Key, formatterResolver);
						writer.WriteNameSeparator();
						formatterWithVerify.Serialize(ref writer, keyValuePair.Value, formatterResolver);
						while (sourceEnumerator.MoveNext())
						{
							writer.WriteValueSeparator();
							KeyValuePair<TKey, TValue> keyValuePair2 = sourceEnumerator.Current;
							objectPropertyNameFormatter.SerializeToPropertyName(ref writer, keyValuePair2.Key, formatterResolver);
							writer.WriteNameSeparator();
							formatterWithVerify.Serialize(ref writer, keyValuePair2.Value, formatterResolver);
						}
					}
				}
				else if (sourceEnumerator.MoveNext())
				{
					KeyValuePair<TKey, TValue> keyValuePair3 = sourceEnumerator.Current;
					TKey tkey = keyValuePair3.Key;
					writer.WriteString(tkey.ToString());
					writer.WriteNameSeparator();
					formatterWithVerify.Serialize(ref writer, keyValuePair3.Value, formatterResolver);
					while (sourceEnumerator.MoveNext())
					{
						writer.WriteValueSeparator();
						KeyValuePair<TKey, TValue> keyValuePair4 = sourceEnumerator.Current;
						tkey = keyValuePair4.Key;
						writer.WriteString(tkey.ToString());
						writer.WriteNameSeparator();
						formatterWithVerify.Serialize(ref writer, keyValuePair4.Value, formatterResolver);
					}
				}
			}
			finally
			{
				sourceEnumerator.Dispose();
			}
			writer.WriteEndObject();
		}

		public TDictionary Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return default(TDictionary);
			}
			IObjectPropertyNameFormatter<TKey> objectPropertyNameFormatter = formatterResolver.GetFormatterWithVerify<TKey>() as IObjectPropertyNameFormatter<TKey>;
			if (objectPropertyNameFormatter == null)
			{
				Type typeFromHandle = typeof(TKey);
				throw new InvalidOperationException(((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " does not support dictionary key deserialize.");
			}
			IJsonFormatter<TValue> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TValue>();
			reader.ReadIsBeginObjectWithVerify();
			TIntermediate tintermediate = this.Create();
			int num = 0;
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				TKey tkey = objectPropertyNameFormatter.DeserializeFromPropertyName(ref reader, formatterResolver);
				reader.ReadIsNameSeparatorWithVerify();
				TValue tvalue = formatterWithVerify.Deserialize(ref reader, formatterResolver);
				this.Add(ref tintermediate, num - 1, tkey, tvalue);
			}
			return this.Complete(ref tintermediate);
		}

		protected abstract TEnumerator GetSourceEnumerator(TDictionary source);

		protected abstract TIntermediate Create();

		protected abstract void Add(ref TIntermediate collection, int index, TKey key, TValue value);

		protected abstract TDictionary Complete(ref TIntermediate intermediateCollection);
	}
}
