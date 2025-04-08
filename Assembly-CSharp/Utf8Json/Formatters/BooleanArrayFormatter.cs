using System;

namespace Utf8Json.Formatters
{
	public sealed class BooleanArrayFormatter : IJsonFormatter<bool[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, bool[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteBoolean(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteBoolean(value[i]);
			}
			writer.WriteEndArray();
		}

		public bool[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			bool[] array = new bool[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<bool>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadBoolean();
			}
			Array.Resize<bool>(ref array, num);
			return array;
		}

		public static readonly BooleanArrayFormatter Default = new BooleanArrayFormatter();
	}
}
