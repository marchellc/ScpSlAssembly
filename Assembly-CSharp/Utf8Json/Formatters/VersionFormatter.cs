using System;

namespace Utf8Json.Formatters
{
	public sealed class VersionFormatter : IJsonFormatter<Version>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, Version value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteString(value.ToString());
		}

		public Version Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new Version(reader.ReadString());
		}

		public static readonly IJsonFormatter<Version> Default = new VersionFormatter();
	}
}
