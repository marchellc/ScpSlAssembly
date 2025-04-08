using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utf8Json.Formatters
{
	internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IEnumerable<TElement>, IEnumerable
	{
		public Grouping(TKey key, IEnumerable<TElement> elements)
		{
			this.key = key;
			this.elements = elements;
		}

		public TKey Key
		{
			get
			{
				return this.key;
			}
		}

		public IEnumerator<TElement> GetEnumerator()
		{
			return this.elements.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private readonly TKey key;

		private readonly IEnumerable<TElement> elements;
	}
}
