using System;
using System.Collections.Generic;

namespace Utf8Json.Formatters
{
	public abstract class DictionaryFormatterBase<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary, TDictionary> where TDictionary : class, IDictionary<TKey, TValue>
	{
		protected override TDictionary Complete(ref TDictionary intermediateCollection)
		{
			return intermediateCollection;
		}
	}
}
