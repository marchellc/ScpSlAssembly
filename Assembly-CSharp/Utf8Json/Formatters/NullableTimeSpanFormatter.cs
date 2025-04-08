using System;

namespace Utf8Json.Formatters
{
	public sealed class NullableTimeSpanFormatter : IJsonFormatter<TimeSpan?>, IJsonFormatter
	{
		public NullableTimeSpanFormatter()
		{
			this.innerFormatter = new TimeSpanFormatter();
		}

		public void Serialize(ref JsonWriter writer, TimeSpan? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			this.innerFormatter.Serialize(ref writer, value.Value, formatterResolver);
		}

		public TimeSpan? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new TimeSpan?(this.innerFormatter.Deserialize(ref reader, formatterResolver));
		}

		private readonly TimeSpanFormatter innerFormatter;
	}
}
