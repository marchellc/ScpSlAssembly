using System;
using System.Collections;

namespace Utf8Json.Formatters
{
	public sealed class NonGenericDictionaryFormatter<T> : IJsonFormatter<T>, IJsonFormatter where T : class, IDictionary, new()
	{
		public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			writer.WriteBeginObject();
			using (IDictionaryEnumerator enumerator = value.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					DictionaryEntry dictionaryEntry = (DictionaryEntry)enumerator.Current;
					writer.WritePropertyName(dictionaryEntry.Key.ToString());
					formatterWithVerify.Serialize(ref writer, dictionaryEntry.Value, formatterResolver);
					while (enumerator.MoveNext())
					{
						writer.WriteValueSeparator();
						DictionaryEntry dictionaryEntry2 = (DictionaryEntry)enumerator.Current;
						writer.WritePropertyName(dictionaryEntry2.Key.ToString());
						formatterWithVerify.Serialize(ref writer, dictionaryEntry2.Value, formatterResolver);
					}
				}
			}
			writer.WriteEndObject();
		}

		public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return default(T);
			}
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			reader.ReadIsBeginObjectWithVerify();
			T t = new T();
			int num = 0;
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				string text = reader.ReadPropertyName();
				object obj = formatterWithVerify.Deserialize(ref reader, formatterResolver);
				t.Add(text, obj);
			}
			return t;
		}
	}
}
