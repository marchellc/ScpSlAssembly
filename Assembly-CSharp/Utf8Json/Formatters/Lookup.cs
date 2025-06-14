using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utf8Json.Formatters;

internal class Lookup<TKey, TElement> : ILookup<TKey, TElement>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
{
	private readonly Dictionary<TKey, IGrouping<TKey, TElement>> groupings;

	public IEnumerable<TElement> this[TKey key] => this.groupings[key];

	public int Count => this.groupings.Count;

	public Lookup(Dictionary<TKey, IGrouping<TKey, TElement>> groupings)
	{
		this.groupings = groupings;
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
}
