﻿using System;
using System.Text;

namespace Utf8Json.Formatters
{
	public sealed class StringBuilderFormatter : IJsonFormatter<StringBuilder>, IJsonFormatter
	{
		public void Serialize(ref JsonWriter writer, StringBuilder value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteString(value.ToString());
		}

		public StringBuilder Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return null;
			}
			return new StringBuilder(reader.ReadString());
		}

		public static readonly IJsonFormatter<StringBuilder> Default = new StringBuilderFormatter();
	}
}
