using System;
using System.Globalization;

namespace Utf8Json.Formatters
{
	public sealed class DateTimeOffsetFormatter : IJsonFormatter<DateTimeOffset>, IJsonFormatter
	{
		public DateTimeOffsetFormatter()
		{
			this.formatString = null;
		}

		public DateTimeOffsetFormatter(string formatString)
		{
			this.formatString = formatString;
		}

		public void Serialize(ref JsonWriter writer, DateTimeOffset value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteString(value.ToString(this.formatString));
		}

		public DateTimeOffset Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			string text = reader.ReadString();
			if (this.formatString == null)
			{
				return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
			}
			return DateTimeOffset.ParseExact(text, this.formatString, CultureInfo.InvariantCulture);
		}

		private readonly string formatString;
	}
}
