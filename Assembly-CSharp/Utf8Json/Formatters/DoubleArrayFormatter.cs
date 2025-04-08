using System;

namespace Utf8Json.Formatters
{
	public sealed class DoubleArrayFormatter : IJsonFormatter<double[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, double[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteDouble(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteDouble(value[i]);
			}
			writer.WriteEndArray();
		}

		public double[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			double[] array = new double[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<double>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadDouble();
			}
			Array.Resize<double>(ref array, num);
			return array;
		}

		public static readonly DoubleArrayFormatter Default = new DoubleArrayFormatter();
	}
}
