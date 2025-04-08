using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class Int64Formatter : IJsonFormatter<long>, IJsonFormatter, IObjectPropertyNameFormatter<long>
	{
		public void Serialize(ref JsonWriter writer, long value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteInt64(value);
		}

		public long Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			return reader.ReadInt64();
		}

		public void SerializeToPropertyName(ref JsonWriter writer, long value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteQuotation();
			writer.WriteInt64(value);
			writer.WriteQuotation();
		}

		public long DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return NumberConverter.ReadInt64(arraySegment.Array, arraySegment.Offset, out num);
		}

		public static readonly Int64Formatter Default = new Int64Formatter();
	}
}
