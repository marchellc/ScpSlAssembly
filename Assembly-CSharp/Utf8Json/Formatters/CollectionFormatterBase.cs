using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class CollectionFormatterBase<TElement, TIntermediate, TEnumerator, TCollection> : IJsonFormatter<TCollection>, IJsonFormatter, IOverwriteJsonFormatter<TCollection> where TEnumerator : IEnumerator<TElement> where TCollection : class, IEnumerable<TElement>
	{
		public void Serialize(ref JsonWriter writer, TCollection value, IJsonFormatterResolver formatterResolver)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteBeginArray();
			IJsonFormatter<TElement> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TElement>();
			TEnumerator sourceEnumerator = this.GetSourceEnumerator(value);
			try
			{
				bool flag = true;
				while (sourceEnumerator.MoveNext())
				{
					if (flag)
					{
						flag = false;
					}
					else
					{
						writer.WriteValueSeparator();
					}
					formatterWithVerify.Serialize(ref writer, sourceEnumerator.Current, formatterResolver);
				}
			}
			finally
			{
				sourceEnumerator.Dispose();
			}
			writer.WriteEndArray();
		}

		public TCollection Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (reader.ReadIsNull())
			{
				return default(TCollection);
			}
			IJsonFormatter<TElement> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TElement>();
			TIntermediate tintermediate = this.Create();
			int num = 0;
			reader.ReadIsBeginArrayWithVerify();
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				this.Add(ref tintermediate, num - 1, formatterWithVerify.Deserialize(ref reader, formatterResolver));
			}
			return this.Complete(ref tintermediate);
		}

		public void DeserializeTo(ref TCollection value, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
		{
			if (this.SupportedOverwriteBehaviour == null)
			{
				value = this.Deserialize(ref reader, formatterResolver);
				return;
			}
			if (reader.ReadIsNull())
			{
				return;
			}
			IJsonFormatter<TElement> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TElement>();
			this.ClearOnOverwriteDeserialize(ref value);
			int num = 0;
			reader.ReadIsBeginArrayWithVerify();
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref num))
			{
				this.AddOnOverwriteDeserialize(ref value, num - 1, formatterWithVerify.Deserialize(ref reader, formatterResolver));
			}
		}

		protected abstract TEnumerator GetSourceEnumerator(TCollection source);

		protected abstract TIntermediate Create();

		protected abstract void Add(ref TIntermediate collection, int index, TElement value);

		protected abstract TCollection Complete(ref TIntermediate intermediateCollection);

		protected virtual CollectionDeserializeToBehaviour? SupportedOverwriteBehaviour
		{
			get
			{
				return null;
			}
		}

		protected virtual void ClearOnOverwriteDeserialize(ref TCollection value)
		{
		}

		protected virtual void AddOnOverwriteDeserialize(ref TCollection collection, int index, TElement value)
		{
		}
	}
}
