using System;

namespace Utf8Json.Formatters
{
	public sealed class NullableCharFormatter : IJsonFormatter<char?>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, char? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			CharFormatter.Default.Serialize(ref writer, value.Value, formatterResolver);
		}

		public char? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new char?(CharFormatter.Default.Deserialize(ref reader, formatterResolver));
		}

		public static readonly NullableCharFormatter Default = new NullableCharFormatter();
	}
}
