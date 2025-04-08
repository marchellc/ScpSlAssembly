using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class ByteFormatter : IJsonFormatter<byte>, IJsonFormatter, IObjectPropertyNameFormatter<byte>
	{
		public void Serialize(ref JsonWriter writer, byte value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteByte(value);
		}

		public byte Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			return reader.ReadByte();
		}

		public void SerializeToPropertyName(ref JsonWriter writer, byte value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteQuotation();
			writer.WriteByte(value);
			writer.WriteQuotation();
		}

		public byte DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return NumberConverter.ReadByte(arraySegment.Array, arraySegment.Offset, out num);
		}

		public static readonly ByteFormatter Default = new ByteFormatter();
	}
}
