using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class CollectionFormatterBase<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection, TCollection> where TCollection : class, IEnumerable<TElement>
	{
		protected sealed override TCollection Complete(ref TCollection intermediateCollection)
		{
			return intermediateCollection;
		}
	}
}
