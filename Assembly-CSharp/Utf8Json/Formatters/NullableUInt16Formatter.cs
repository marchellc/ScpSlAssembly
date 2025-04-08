using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableUInt16Formatter : IJsonFormatter<ushort?>, IJsonFormatter, IObjectPropertyNameFormatter<ushort?>
	{
		public void Serialize(ref JsonWriter writer, ushort? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteUInt16(value.Value);
		}

		public ushort? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new ushort?(reader.ReadUInt16());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, ushort? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteUInt16(value.Value);
			writer.WriteQuotation();
		}

		public ushort? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new ushort?(NumberConverter.ReadUInt16(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableUInt16Formatter Default = new NullableUInt16Formatter();
	}
}
