using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters;

public sealed class UnixTimestampDateTimeFormatter : IJsonFormatter<DateTime>, IJsonFormatter
{
	private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public void Serialize(ref JsonWriter writer, DateTime value, IJsonFormatterResolver formatterResolver)
	{
		long value2 = (long)(value.ToUniversalTime() - UnixTimestampDateTimeFormatter.UnixEpoch).TotalSeconds;
		writer.WriteQuotation();
		writer.WriteInt64(value2);
		writer.WriteQuotation();
	}

	public DateTime Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
		int readCount;
		ulong num = NumberConverter.ReadUInt64(arraySegment.Array, arraySegment.Offset, out readCount);
		DateTime unixEpoch = UnixTimestampDateTimeFormatter.UnixEpoch;
		return unixEpoch.AddSeconds(num);
	}
}
