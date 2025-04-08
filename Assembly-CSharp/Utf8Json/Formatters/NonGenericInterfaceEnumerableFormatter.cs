using System;
using System.Collections;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public sealed class NonGenericInterfaceEnumerableFormatter : IJsonFormatter<IEnumerable>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, IEnumerable value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			IJsonFormatter<object> formatterWithVerify = formatterResolver.GetFormatterWithVerify<object>();
			writer.WriteBeginArray();
			int num = 0;
			foreach (object obj in value)
			{
				if (num != 0)
				{
					writer.WriteValueSeparator();
				}
				formatterWithVerify.Serialize(ref writer, obj, formatterResolver);
			}
			writer.WriteEndArray();
		}

		public IEnumerable Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
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

		public static readonly IJsonFormatter<IEnumerable> Default = new NonGenericInterfaceEnumerableFormatter();
	}
}
