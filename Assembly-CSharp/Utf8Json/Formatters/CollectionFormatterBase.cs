using System.Collections.Generic;

namespace Utf8Json.Formatters;

public abstract class CollectionFormatterBase<TElement, TIntermediate, TEnumerator, TCollection> : IJsonFormatter<TCollection>, IJsonFormatter, IOverwriteJsonFormatter<TCollection> where TEnumerator : IEnumerator<TElement> where TCollection : class, IEnumerable<TElement>
{
	protected virtual CollectionDeserializeToBehaviour? SupportedOverwriteBehaviour => null;

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
			return null;
		}
		IJsonFormatter<TElement> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TElement>();
		TIntermediate collection = this.Create();
		int count = 0;
		reader.ReadIsBeginArrayWithVerify();
		while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref count))
		{
			this.Add(ref collection, count - 1, formatterWithVerify.Deserialize(ref reader, formatterResolver));
		}
		return this.Complete(ref collection);
	}

	public void DeserializeTo(ref TCollection value, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
	{
		if (!this.SupportedOverwriteBehaviour.HasValue)
		{
			value = this.Deserialize(ref reader, formatterResolver);
		}
		else if (!reader.ReadIsNull())
		{
			IJsonFormatter<TElement> formatterWithVerify = formatterResolver.GetFormatterWithVerify<TElement>();
			this.ClearOnOverwriteDeserialize(ref value);
			int count = 0;
			reader.ReadIsBeginArrayWithVerify();
			while (!reader.ReadIsEndArrayWithSkipValueSeparator(ref count))
			{
				this.AddOnOverwriteDeserialize(ref value, count - 1, formatterWithVerify.Deserialize(ref reader, formatterResolver));
			}
		}
	}

	protected abstract TEnumerator GetSourceEnumerator(TCollection source);

	protected abstract TIntermediate Create();

	protected abstract void Add(ref TIntermediate collection, int index, TElement value);

	protected abstract TCollection Complete(ref TIntermediate intermediateCollection);

	protected virtual void ClearOnOverwriteDeserialize(ref TCollection value)
	{
	}

	protected virtual void AddOnOverwriteDeserialize(ref TCollection collection, int index, TElement value)
	{
	}
}
public abstract class CollectionFormatterBase<TElement, TIntermediate, TCollection> : CollectionFormatterBase<TElement, TIntermediate, IEnumerator<TElement>, TCollection> where TCollection : class, IEnumerable<TElement>
{
	protected override IEnumerator<TElement> GetSourceEnumerator(TCollection source)
	{
		return source.GetEnumerator();
	}
}
public abstract class CollectionFormatterBase<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection, TCollection> where TCollection : class, IEnumerable<TElement>
{
	protected sealed override TCollection Complete(ref TCollection intermediateCollection)
	{
		return intermediateCollection;
	}
}
