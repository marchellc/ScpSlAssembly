using System;

namespace Utf8Json.Formatters
{
	public sealed class AnonymousFormatter<T> : IJsonFormatter<T>, IJsonFormatter
	{
		public AnonymousFormatter(JsonSerializeAction<T> serialize, JsonDeserializeFunc<T> deserialize)
		{
			this.serialize = serialize;
			this.deserialize = deserialize;
		}

		public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
		{
			if (this.serialize == null)
			{
				throw new InvalidOperationException(base.GetType().Name + " does not support Serialize.");
			}
			this.serialize(ref writer, value, formatterResolver);
		}

		public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (this.deserialize == null)
			{
				throw new InvalidOperationException(base.GetType().Name + " does not support Deserialize.");
			}
			return this.deserialize(ref reader, formatterResolver);
		}

		private readonly JsonSerializeAction<T> serialize;

		private readonly JsonDeserializeFunc<T> deserialize;
	}
}
