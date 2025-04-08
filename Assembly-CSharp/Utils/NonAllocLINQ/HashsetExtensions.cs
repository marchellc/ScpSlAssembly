using System;
using System.Collections.Generic;

namespace Utils.NonAllocLINQ
{
	public static class HashsetExtensions
	{
		public static void ForEach<T>(this HashSet<T> target, Action<T> action)
		{
			foreach (T t in target)
			{
				action(t);
			}
		}

		public static bool Any<T>(this HashSet<T> target, Func<T, bool> condition)
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

		public static int Count<T>(this HashSet<T> target, Func<T, bool> condition)
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

		public static bool All<T>(this HashSet<T> target, Func<T, bool> condition, bool emptyResult = true)
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

		public static T FirstOrDefault<T>(this HashSet<T> target, Func<T, bool> condition, T defaultRet)
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

		public static bool TryGetFirst<T>(this HashSet<T> target, Func<T, bool> condition, out T first)
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
	}
}
