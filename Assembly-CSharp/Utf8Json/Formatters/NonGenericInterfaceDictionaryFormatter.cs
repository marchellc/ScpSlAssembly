using System;
using System.Collections;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public sealed class NonGenericInterfaceDictionaryFormatter : IJsonFormatter<IDictionary>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, IDictionary value, IJsonFormatterResolver formatterResolver)
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

		public IDictionary Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			reader.ReadIsBeginObjectWithVerify();
			Dictionary<object, object> dictionary = new Dictionary<object, object>();
			int num = 0;
			while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref num))
			{
				string text = reader.ReadPropertyName();
				object obj = formatterWithVerify.Deserialize(ref reader, formatterResolver);
				dictionary.Add(text, obj);
			}
			return dictionary;
		}

		public static readonly IJsonFormatter<IDictionary> Default = new NonGenericInterfaceDictionaryFormatter();
	}
}
