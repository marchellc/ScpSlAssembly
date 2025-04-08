using System;
using Utf8Json.Internal;

namespace Utf8Json.Formatters
{
	public sealed class UnixTimestampDateTimeFormatter : IJsonFormatter<DateTime>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, DateTime value, IJsonFormatterResolver formatterResolver)
		{
			long num = (long)(value.ToUniversalTime() - UnixTimestampDateTimeFormatter.UnixEpoch).TotalSeconds;
			writer.WriteQuotation();
			writer.WriteInt64(num);
			writer.WriteQuotation();
		}

		public DateTime Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			ArraySegment<byte> arraySegment = reader.ReadStringSegmentUnsafe();
			int num2;
			ulong num = NumberConverter.ReadUInt64(arraySegment.Array, arraySegment.Offset, out num2);
			return UnixTimestampDateTimeFormatter.UnixEpoch.AddSeconds(num);
		}

		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}
}
