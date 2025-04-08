using System;

namespace Utf8Json.Formatters
{
	public sealed class SingleArrayFormatter : IJsonFormatter<float[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, float[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteSingle(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteSingle(value[i]);
			}
			writer.WriteEndArray();
		}

		public float[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			float[] array = new float[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<float>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadSingle();
			}
			Array.Resize<float>(ref array, num);
			return array;
		}

		public static readonly SingleArrayFormatter Default = new SingleArrayFormatter();
	}
}
