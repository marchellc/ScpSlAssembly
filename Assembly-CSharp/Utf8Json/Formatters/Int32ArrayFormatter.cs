using System;

namespace Utf8Json.Formatters
{
	public sealed class Int32ArrayFormatter : IJsonFormatter<int[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, int[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteInt32(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteInt32(value[i]);
			}
			writer.WriteEndArray();
		}

		public int[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			int[] array = new int[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<int>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadInt32();
			}
			Array.Resize<int>(ref array, num);
			return array;
		}

		public static readonly Int32ArrayFormatter Default = new Int32ArrayFormatter();
	}
}
