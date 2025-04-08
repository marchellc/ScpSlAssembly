using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableUInt64Formatter : IJsonFormatter<ulong?>, IJsonFormatter, IObjectPropertyNameFormatter<ulong?>
	{
		public void Serialize(ref JsonWriter writer, ulong? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteUInt64(value.Value);
		}

		public ulong? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new ulong?(reader.ReadUInt64());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, ulong? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteUInt64(value.Value);
			writer.WriteQuotation();
		}

		public ulong? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new ulong?(NumberConverter.ReadUInt64(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableUInt64Formatter Default = new NullableUInt64Formatter();
	}
}
