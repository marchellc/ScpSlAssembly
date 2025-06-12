using System;

namespace Utf8Json.Formatters;

public sealed class NullableDateTimeOffsetFormatter : IJsonFormatter<DateTimeOffset?>, IJsonFormatter
{
	private readonly DateTimeOffsetFormatter innerFormatter;

	public NullableDateTimeOffsetFormatter()
	{
		this.innerFormatter = new DateTimeOffsetFormatter();
	}

	public NullableDateTimeOffsetFormatter(string formatString)
	{
		this.innerFormatter = new DateTimeOffsetFormatter(formatString);
	}

	public void Serialize(ref JsonWriter writer, DateTimeOffset? value, IJsonFormatterResolver formatterResolver)
	{
		if (!value.HasValue)
		{
			writer.WriteNull();
		}
		else
		{
			this.innerFormatter.Serialize(ref writer, value.Value, formatterResolver);
		}
	}

	public DateTimeOffset? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (reader.ReadIsNull())
		{
			return null;
		}
		return this.innerFormatter.Deserialize(ref reader, formatterResolver);
	}
}
