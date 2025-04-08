using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableBooleanFormatter : IJsonFormatter<bool?>, IJsonFormatter, IObjectPropertyNameFormatter<bool?>
	{
		public void Serialize(ref JsonWriter writer, bool? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBoolean(value.Value);
		}

		public bool? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new bool?(reader.ReadBoolean());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, bool? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteBoolean(value.Value);
			writer.WriteQuotation();
		}

		public bool? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new bool?(NumberConverter.ReadBoolean(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableBooleanFormatter Default = new NullableBooleanFormatter();
	}
}
