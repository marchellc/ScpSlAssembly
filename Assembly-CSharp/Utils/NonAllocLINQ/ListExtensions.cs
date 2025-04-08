using System;
using System.Collections.Generic;

namespace Utils.NonAllocLINQ
{
	public static class ListExtensions
	{
		public static void ForEach<T>(this List<T> target, Action<T> action)
		{
			foreach (T t in target)
			{
				action(t);
			}
		}

		public static bool Any<T>(this List<T> target, Func<T, bool> condition)
		{
			foreach (T t in target)
			{
				if (condition(t))
				{
					return true;
				}
			}
			return false;
		}

		public static int Count<T>(this List<T> target, Func<T, bool> condition)
		{
			int num = 0;
			foreach (T t in target)
			{
				if (condition(t))
				{
					num++;
				}
			}
			return num;
		}

		public static bool All<T>(this List<T> target, Func<T, bool> condition, bool emptyResult = true)
		{
			foreach (T t in target)
			{
				if (!condition(t))
				{
					return false;
				}
				emptyResult = true;
			}
			return emptyResult;
		}

		public static T FirstOrDefault<T>(this List<T> target, Func<T, bool> condition, T defaultRet)
		{
			foreach (T t in target)
			{
				if (condition(t))
				{
					return t;
				}
			}
			return defaultRet;
		}

		public static bool TryGetFirst<T>(this List<T> target, Func<T, bool> condition, out T first)
		{
			foreach (T t in target)
			{
				if (condition(t))
				{
					first = t;
					return true;
				}
			}
			first = default(T);
			return false;
		}

		public static bool TryGetFirstIndex<T>(this List<T> target, Func<T, bool> condition, out int indexOfFirst)
		{
			int count = target.Count;
			for (int i = 0; i < count; i++)
			{
				T t = target[i];
				if (condition(t))
				{
					indexOfFirst = i;
					return true;
				}
			}
			indexOfFirst = -1;
			return false;
		}

		public static bool AddIfNotContains<T>(this List<T> target, T element)
		{
			if (target.Contains(element))
			{
				return false;
			}
			target.Add(element);
			return true;
		}
	}
}
