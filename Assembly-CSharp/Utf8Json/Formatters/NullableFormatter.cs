using System;

namespace Utf8Json.Formatters
{
	public sealed class NullableFormatter<T> : IJsonFormatter<T?>, IJsonFormatter where T : struct
	{
		public void Serialize(ref JsonWriter writer, T? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			formatterResolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, formatterResolver);
		}

		public T? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new T?(formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver));
		}
	}
}
