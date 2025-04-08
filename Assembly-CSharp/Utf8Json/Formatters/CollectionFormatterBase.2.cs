using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class CollectionFormatterBase<TElement, TIntermediate, TCollection> : CollectionFormatterBase<TElement, TIntermediate, IEnumerator<TElement>, TCollection> where TCollection : class, IEnumerable<TElement>
	{
		protected override IEnumerator<TElement> GetSourceEnumerator(TCollection source)
		{
			return source.GetEnumerator();
		}
	}
}
