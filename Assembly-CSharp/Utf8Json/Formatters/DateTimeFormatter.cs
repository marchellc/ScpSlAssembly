using System;
using System.Globalization;

namespace Utf8Json.Formatters
{
	public sealed class DateTimeFormatter : IJsonFormatter<DateTime>, IJsonFormatter
	{
		public DateTimeFormatter()
		{
			this.formatString = null;
		}

		public DateTimeFormatter(string formatString)
		{
			this.formatString = formatString;
		}

		public void Serialize(ref JsonWriter writer, DateTime value, IJsonFormatterResolver formatterResolver)
		{
			writer.WriteString(value.ToString(this.formatString));
		}

		public DateTime Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			string text = reader.ReadString();
			if (this.formatString == null)
			{
				return DateTime.Parse(text, CultureInfo.InvariantCulture);
			}
			return DateTime.ParseExact(text, this.formatString, CultureInfo.InvariantCulture);
		}

		private readonly string formatString;
	}
}
