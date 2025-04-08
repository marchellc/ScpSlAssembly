using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableInt32Formatter : IJsonFormatter<int?>, IJsonFormatter, IObjectPropertyNameFormatter<int?>
	{
		public void Serialize(ref JsonWriter writer, int? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteInt32(value.Value);
		}

		public int? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new int?(reader.ReadInt32());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, int? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteInt32(value.Value);
			writer.WriteQuotation();
		}

		public int? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new int?(NumberConverter.ReadInt32(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableInt32Formatter Default = new NullableInt32Formatter();
	}
}
