using System;

namespace Utf8Json.Formatters
{
	public sealed class UInt32ArrayFormatter : IJsonFormatter<uint[]>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, uint[] value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			if (value.Length != 0)
			{
				writer.WriteUInt32(value[0]);
			}
			for (int i = 1; i < value.Length; i++)
			{
				writer.WriteValueSeparator();
				writer.WriteUInt32(value[i]);
			}
			writer.WriteEndArray();
		}

		public uint[] Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			reader.ReadIsBeginArrayWithVerify();
			uint[] array = new uint[4];
			int num = 0;
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				if (array.Length < num)
				{
					Array.Resize<uint>(ref array, num * 2);
				}
				array[num - 1] = reader.ReadUInt32();
			}
			Array.Resize<uint>(ref array, num);
			return array;
		}

		public static readonly UInt32ArrayFormatter Default = new UInt32ArrayFormatter();
	}
}
