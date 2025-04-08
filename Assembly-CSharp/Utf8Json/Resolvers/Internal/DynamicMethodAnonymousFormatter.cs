using System;

namespace Utf8Json.Resolvers.Internal
{
	internal class DynamicMethodAnonymousFormatter<T> : IJsonFormatter<T>, IJsonFormatter
	{
		public DynamicMethodAnonymousFormatter(byte[][] stringByteKeysField, object[] serializeCustomFormatters, object[] deserializeCustomFormatters, AnonymousJsonSerializeAction<T> serialize, AnonymousJsonDeserializeFunc<T> deserialize)
		{
			this.stringByteKeysField = stringByteKeysField;
			this.serializeCustomFormatters = serializeCustomFormatters;
			this.deserializeCustomFormatters = deserializeCustomFormatters;
			this.serialize = serialize;
			this.deserialize = deserialize;
		}

		public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
		{
			if (this.serialize == null)
			{
				throw new InvalidOperationException(base.GetType().Name + " does not support Serialize.");
			}
			this.serialize(this.stringByteKeysField, this.serializeCustomFormatters, ref writer, value, formatterResolver);
		}

		public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (this.deserialize == null)
			{
				throw new InvalidOperationException(base.GetType().Name + " does not support Deserialize.");
			}
			return this.deserialize(this.deserializeCustomFormatters, ref reader, formatterResolver);
		}

		private readonly byte[][] stringByteKeysField;

		private readonly object[] serializeCustomFormatters;

		private readonly object[] deserializeCustomFormatters;

		private readonly AnonymousJsonSerializeAction<T> serialize;

		private readonly AnonymousJsonDeserializeFunc<T> deserialize;
	}
}
