using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utf8Json.Formatters
{
	internal class Lookup<TKey, TElement> : ILookup<TKey, TElement>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
	{
		public Lookup(Dictionary<TKey, IGrouping<TKey, TElement>> groupings)
		{
			this.groupings = groupings;
		}

		public IEnumerable<TElement> this[TKey key]
		{
			get
			{
				return this.groupings[key];
			}
		}

		public int Count
		{
			get
			{
				return this.groupings.Count;
			}
		}

		public bool Contains(TKey key)
		{
			return this.groupings.ContainsKey(key);
		}

		public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
		{
			return this.groupings.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.groupings.Values.GetEnumerator();
		}

		private readonly Dictionary<TKey, IGrouping<TKey, TElement>> groupings;
	}
}
