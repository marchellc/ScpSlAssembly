using System;

namespace Utf8Json.Formatters;

public sealed class StaticNullableFormatter<T> : IJsonFormatter<T?>, IJsonFormatter where T : struct
{
	private readonly IJsonFormatter<T> underlyingFormatter;

	public StaticNullableFormatter(IJsonFormatter<T> underlyingFormatter)
	{
		this.underlyingFormatter = underlyingFormatter;
	}

	public StaticNullableFormatter(Type formatterType, object[] formatterArguments)
	{
		try
		{
			this.underlyingFormatter = (IJsonFormatter<T>)Activator.CreateInstance(formatterType, formatterArguments);
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException("Can not create formatter from JsonFormatterAttribute, check the target formatter is public and has constructor with right argument. FormatterType:" + formatterType.Name, innerException);
		}
	}

	public void Serialize(ref JsonWriter writer, T? value, IJsonFormatterResolver formatterResolver)
	{
		if (!value.HasValue)
		{
			writer.WriteNull();
		}
		else
		{
			this.underlyingFormatter.Serialize(ref writer, value.Value, formatterResolver);
		}
	}

	public T? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (reader.ReadIsNull())
		{
			return null;
		}
		return this.underlyingFormatter.Deserialize(ref reader, formatterResolver);
	}
}
