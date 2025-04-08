using System;

namespace Utf8Json.Formatters
{
	public sealed class NullableDateTimeFormatter : IJsonFormatter<DateTime?>, IJsonFormatter
	{
		public NullableDateTimeFormatter()
		{
			this.innerFormatter = new DateTimeFormatter();
		}

		public NullableDateTimeFormatter(string formatString)
		{
			this.innerFormatter = new DateTimeFormatter(formatString);
		}

		public void Serialize(ref JsonWriter writer, DateTime? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			this.innerFormatter.Serialize(ref writer, value.Value, formatterResolver);
		}

		public DateTime? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new DateTime?(this.innerFormatter.Deserialize(ref reader, formatterResolver));
		}

		private readonly DateTimeFormatter innerFormatter;
	}
}
