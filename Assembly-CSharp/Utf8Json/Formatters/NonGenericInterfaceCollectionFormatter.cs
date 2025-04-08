using System;
using System.Collections;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public sealed class NonGenericInterfaceCollectionFormatter : IJsonFormatter<ICollection>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, ICollection value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			writer.WriteBeginArray();
			using (IEnumerator enumerator = value.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					formatterWithVerify.Serialize(ref writer, enumerator.Current, formatterResolver);
					while (enumerator.MoveNext())
					{
						writer.WriteValueSeparator();
						formatterWithVerify.Serialize(ref writer, enumerator.Current, formatterResolver);
					}
				}
			}
			writer.WriteEndArray();
		}

		public ICollection Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			int num = 0;
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			List<object> list = new List<object>();
			reader.ReadIsBeginArrayWithVerify();
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				list.Add(formatterWithVerify.Deserialize(ref reader, formatterResolver));
			}
			return list;
		}

		public static readonly IJsonFormatter<ICollection> Default = new NonGenericInterfaceCollectionFormatter();
	}
}
