using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utf8Json.Formatters;

internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IEnumerable<TElement>, IEnumerable
{
	private readonly TKey key;

	private readonly IEnumerable<TElement> elements;

	public TKey Key => this.key;

	public Grouping(TKey key, IEnumerable<TElement> elements)
	{
		this.key = key;
		this.elements = elements;
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		return this.elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}
}
