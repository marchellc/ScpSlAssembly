using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableSByteFormatter : IJsonFormatter<sbyte?>, IJsonFormatter, IObjectPropertyNameFormatter<sbyte?>
	{
		public void Serialize(ref JsonWriter writer, sbyte? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteSByte(value.Value);
		}

		public sbyte? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new sbyte?(reader.ReadSByte());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, sbyte? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteSByte(value.Value);
			writer.WriteQuotation();
		}

		public sbyte? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new sbyte?(NumberConverter.ReadSByte(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableSByteFormatter Default = new NullableSByteFormatter();
	}
}
