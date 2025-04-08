﻿using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class NullableSingleFormatter : IJsonFormatter<float?>, IJsonFormatter, IObjectPropertyNameFormatter<float?>
	{
		public void Serialize(ref JsonWriter writer, float? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteSingle(value.Value);
		}

		public float? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new float?(reader.ReadSingle());
		}

		public void SerializeToPropertyName(ref JsonWriter writer, float? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteQuotation();
			writer.WriteSingle(value.Value);
			writer.WriteQuotation();
		}

		public float? DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentRaw();
			int num;
			return new float?(NumberConverter.ReadSingle(arraySegment.Array, arraySegment.Offset, out num));
		}

		public static readonly NullableSingleFormatter Default = new NullableSingleFormatter();
	}
}
