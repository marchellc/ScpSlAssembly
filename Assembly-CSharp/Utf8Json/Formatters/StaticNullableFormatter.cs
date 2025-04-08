using System;

namespace Utf8Json.Formatters
{
	public sealed class StaticNullableFormatter<T> : IJsonFormatter<T?>, IJsonFormatter where T : struct
	{
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
			catch (Exception ex)
			{
				throw new InvalidOperationException("Can not create formatter from JsonFormatterAttribute, check the target formatter is public and has constructor with right argument. FormatterType:" + formatterType.Name, ex);
			}
		}

		public void Serialize(ref JsonWriter writer, T? value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			this.underlyingFormatter.Serialize(ref writer, value.Value, formatterResolver);
		}

		public T? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new T?(this.underlyingFormatter.Deserialize(ref reader, formatterResolver));
		}

		private readonly IJsonFormatter<T> underlyingFormatter;
	}
}
