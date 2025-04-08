﻿using System;
using System.Collections.Generic;

namespace Utils.NonAllocLINQ
{
	public static class DictionaryExtensions
	{
		public static void ForEach<TKey, TVal>(this Dictionary<TKey, TVal> target, Action<KeyValuePair<TKey, TVal>> action)
		{
			foreach (KeyValuePair<TKey, TVal> keyValuePair in target)
			{
				action(keyValuePair);
			}
		}

		public static void FromArray<TKey, TArrItem>(this Dictionary<TKey, TArrItem> target, TArrItem[] array, Func<TArrItem, TKey> selector)
		{
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				TArrItem tarrItem = array[i];
				target[selector(tarrItem)] = tarrItem;
			}
		}

		public static void ForEachKey<TKey, TVal>(this Dictionary<TKey, TVal> target, Action<TKey> action)
		{
			target.ForEach(delegate(KeyValuePair<TKey, TVal> x)
			{
				action(x.Key);
			});
		}

		public static void ForEachValue<TKey, TVal>(this Dictionary<TKey, TVal> target, Action<TVal> action)
		{
			target.ForEach(delegate(KeyValuePair<TKey, TVal> x)
			{
				action(x.Value);
			});
		}

		public static int Count<TKey, TVal>(this Dictionary<TKey, TVal> target, Func<KeyValuePair<TKey, TVal>, bool> condition)
		{
			int num = 0;
			foreach (KeyValuePair<TKey, TVal> keyValuePair in target)
			{
				if (condition(keyValuePair))
				{
					num++;
				}
			}
			return num;
		}

		public static bool Any<TKey, TVal>(this Dictionary<TKey, TVal> target, Func<KeyValuePair<TKey, TVal>, bool> condition)
		{
			foreach (KeyValuePair<TKey, TVal> keyValuePair in target)
			{
				if (condition(keyValuePair))
				{
					return true;
				}
			}
			return false;
		}

		public static bool All<TKey, TVal>(this Dictionary<TKey, TVal> target, Func<KeyValuePair<TKey, TVal>, bool> condition)
		{
			foreach (KeyValuePair<TKey, TVal> keyValuePair in target)
			{
				if (!condition(keyValuePair))
				{
					return false;
				}
			}
			return true;
		}
	}
}
