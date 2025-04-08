using System;

namespace Utf8Json.Formatters
{
	public sealed class CharArrayFormatter : IJsonFormatter<char[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, char[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				CharFormatter.Default.Serialize(ref writer, value[0], formatterResolver);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				CharFormatter.Default.Serialize(ref writer, value[i], formatterResolver);
			}
			writer.WriteEndArray();
		}

		public char[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			char[] array = new char[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<char>(ref array, num * 2);
				}
				array[num - 1] = CharFormatter.Default.Deserialize(ref reader, formatterResolver);
			}
			Array.Resize<char>(ref array, num);
			return array;
		}

		public static readonly CharArrayFormatter Default = new CharArrayFormatter();
	}
}
