using System;

namespace Utf8Json.Formatters
{
	public sealed class Int16ArrayFormatter : IJsonFormatter<short[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, short[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteInt16(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteInt16(value[i]);
			}
			writer.WriteEndArray();
		}

		public short[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			short[] array = new short[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<short>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadInt16();
			}
			Array.Resize<short>(ref array, num);
			return array;
		}

		public static readonly Int16ArrayFormatter Default = new Int16ArrayFormatter();
	}
}
