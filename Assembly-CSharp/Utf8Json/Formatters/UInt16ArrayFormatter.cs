using System;

namespace Utf8Json.Formatters
{
	public sealed class UInt16ArrayFormatter : IJsonFormatter<ushort[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, ushort[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteUInt16(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteUInt16(value[i]);
			}
			writer.WriteEndArray();
		}

		public ushort[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			ushort[] array = new ushort[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<ushort>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadUInt16();
			}
			Array.Resize<ushort>(ref array, num);
			return array;
		}

		public static readonly UInt16ArrayFormatter Default = new UInt16ArrayFormatter();
	}
}
