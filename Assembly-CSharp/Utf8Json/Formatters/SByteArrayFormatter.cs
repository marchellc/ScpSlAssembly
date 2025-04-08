using System;

namespace Utf8Json.Formatters
{
	public sealed class SByteArrayFormatter : IJsonFormatter<sbyte[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, sbyte[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteSByte(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteSByte(value[i]);
			}
			writer.WriteEndArray();
		}

		public sbyte[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			sbyte[] array = new sbyte[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<sbyte>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadSByte();
			}
			Array.Resize<sbyte>(ref array, num);
			return array;
		}

		public static readonly SByteArrayFormatter Default = new SByteArrayFormatter();
	}
}
