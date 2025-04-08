using System;

namespace Utf8Json
{
	public static class JsonFormatterExtensions
	{
		public static string ToJsonString<T>(this IJsonFormatter<T> formatter, T value, IJsonFormatterResolver formatterResolver)
		{
			JsonWriter jsonWriter = default(JsonWriter);
			formatter.Serialize(ref jsonWriter, value, formatterResolver);
			return jsonWriter.ToString();
		}
	}
}
