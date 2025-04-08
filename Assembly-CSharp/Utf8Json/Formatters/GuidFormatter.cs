using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class GuidFormatter : IJsonFormatter<Guid>, IJsonFormatter, IObjectPropertyNameFormatter<Guid>
	{
		public void Serialize(ref JsonWriter writer, Guid value, IJsonFormatterResolver formatterResolver)
		{
			writer.EnsureCapacity(38);
			writer.WriteRawUnsafe(34);
			ArraySegment<byte> buffer = writer.GetBuffer();
			new GuidBits(ref value).Write(buffer.Array, writer.CurrentOffset);
			writer.AdvanceOffset(36);
			writer.WriteRawUnsafe(34);
		}

		public Guid Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
			return new GuidBits(ref arraySegment).Value;
		}

		public void SerializeToPropertyName(ref JsonWriter writer, Guid value, IJsonFormatterResolver formatterResolver)
		{
			this.Serialize(ref writer, value, formatterResolver);
		}

		public Guid DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			return this.Deserialize(ref reader, formatterResolver);
		}

		public static readonly IJsonFormatter<Guid> Default = new GuidFormatter();
	}
}
