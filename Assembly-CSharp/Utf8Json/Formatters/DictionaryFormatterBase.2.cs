using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TDictionary> : DictionaryFormatterBase<TKey, TValue, TIntermediate, IEnumerator<KeyValuePair<TKey, TValue>>, TDictionary> where TDictionary : class, IEnumerable<KeyValuePair<TKey, TValue>>
	{
		protected override IEnumerator<KeyValuePair<TKey, TValue>> GetSourceEnumerator(TDictionary source)
		{
			return source.GetEnumerator();
		}
	}
}
