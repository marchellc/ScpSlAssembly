using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

public static class CollectionExtensions
{
	public static void ShuffleQueue<T>(this Queue<T> queue, global::System.Random rng = null)
	{
		if (rng == null)
		{
			rng = new global::System.Random();
		}
		List<T> list = ListPool<T>.Shared.Rent(queue.Count);
		while (queue.Count > 0)
		{
			list.Add(queue.Dequeue());
		}
		queue.Clear();
		while (list.Count > 0)
		{
			int num = rng.Next(list.Count);
			queue.Enqueue(list[num]);
			list.RemoveAt(num);
		}
		ListPool<T>.Shared.Return(list);
	}

	public static void ShuffleList<T>(this IList<T> list, global::System.Random rng = null)
	{
		if (rng == null)
		{
			rng = new global::System.Random();
		}
		int i = list.Count;
		while (i > 1)
		{
			i--;
			int num = rng.Next(i + 1);
			T t = list[num];
			list[num] = list[i];
			list[i] = t;
		}
	}

	public static void ShuffleListSecure<T>(this IList<T> list)
	{
		using (RNGCryptoServiceProvider rngcryptoServiceProvider = new RNGCryptoServiceProvider())
		{
			int i = list.Count;
			while (i > 1)
			{
				byte[] array = new byte[1];
				do
				{
					rngcryptoServiceProvider.GetBytes(array);
				}
				while ((int)array[0] >= i * (255 / i));
				int num = (int)array[0] % i;
				i--;
				T t = list[num];
				list[num] = list[i];
				list[i] = t;
			}
		}
	}

	public static TReturn[] OfType<TOriginal, TReturn>(this TOriginal[] original)
	{
		List<TReturn> list = ListPool<TReturn>.Shared.Rent();
		foreach (TOriginal toriginal in original)
		{
			if (toriginal is TReturn)
			{
				TReturn treturn = toriginal as TReturn;
				list.Add(treturn);
			}
		}
		TReturn[] array = list.ToArray();
		ListPool<TReturn>.Shared.Return(list);
		return array;
	}

	public static bool IsEmpty(this Array array)
	{
		return array.Length == 0;
	}

	public static bool IsEmpty<T>(this T[] array)
	{
		return array.Length == 0;
	}

	public static bool IsEmpty<T>(this ArraySegment<T> array)
	{
		return array.Count == 0;
	}

	public static bool IsEmpty<T>(this List<T> list)
	{
		return list.Count == 0;
	}

	public static bool IsEmpty<T>(this Queue<T> queue)
	{
		return queue.Count == 0;
	}

	public static bool IsEmpty<T>(this Stack<T> stack)
	{
		return stack.Count == 0;
	}

	public static bool IsEmpty<T>(this HashSet<T> set)
	{
		return set.Count == 0;
	}

	public static bool IsEmpty<T>(this SortedSet<T> set)
	{
		return set.Count == 0;
	}

	public static bool IsEmpty<T>(this SyncList<T> list)
	{
		return list.Count == 0;
	}

	public static bool IsEmpty<T>(this SyncSet<T> set)
	{
		return set.Count == 0;
	}

	public static bool IsEmpty<TKey, TValue>(this SyncDictionary<TKey, TValue> dictionary)
	{
		return dictionary.Count == 0;
	}

	public static bool IsEmpty<T>(this ICollection<T> collection)
	{
		return collection.Count == 0;
	}

	public static bool IsEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
	{
		return dictionary.Count == 0;
	}

	public static bool IsEmpty<T>(this IEnumerable<T> iEnumerable)
	{
		return !iEnumerable.Any<T>();
	}

	public static void EnsureCapacity<T>(this List<T> list, int capacity)
	{
		if (list.Capacity < capacity)
		{
			list.Capacity = capacity;
		}
	}

	public static int IndexOf<T>(this T[] array, T obj)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (EqualityComparer<T>.Default.Equals(array[i], obj))
			{
				return i;
			}
		}
		return -1;
	}

	public static int LastIndexOf<T>(this T[] array, T obj)
	{
		for (int i = array.Length - 1; i >= 0; i--)
		{
			if (EqualityComparer<T>.Default.Equals(array[i], obj))
			{
				return i;
			}
		}
		return -1;
	}

	public static bool Contains<T>(this T[] array, T obj)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (EqualityComparer<T>.Default.Equals(array[i], obj))
			{
				return true;
			}
		}
		return false;
	}

	public static void ForEach<T>(this T[] array, Action<T> obj)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (obj != null)
			{
				obj(array[i]);
			}
		}
	}

	public static void Reverse<T>(this T[] array)
	{
		int i = 0;
		int num = array.Length;
		while (i < num)
		{
			T t = array[i];
			array[i] = array[num];
			array[num] = t;
			i++;
			num--;
		}
	}

	public static bool CheckIdentical<T>(ICollection<T> left, ICollection<T> right, Func<T, T, bool> isEqual)
	{
		bool flag = left == null;
		bool flag2 = right == null;
		if (flag != flag2)
		{
			return false;
		}
		if (flag && flag2)
		{
			return true;
		}
		int count = left.Count;
		int count2 = right.Count;
		if (count != count2)
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			if (!isEqual(left.ElementAt(i), right.ElementAt(i)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool Contains(this string[] array, string str, StringComparison comparison = StringComparison.Ordinal)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (string.Equals(array[i], str, comparison))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Contains(this List<string> list, string str, StringComparison comparison = StringComparison.Ordinal)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (string.Equals(list[i], str, comparison))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGet<T>(this T[] array, int index, out T element)
	{
		if (index > -1 && index < array.Length)
		{
			element = array[index];
			return true;
		}
		element = default(T);
		return false;
	}

	public static bool TryGet<T>(this List<T> list, int index, out T element)
	{
		if (index > -1 && index < list.Count)
		{
			element = list[index];
			return true;
		}
		element = default(T);
		return false;
	}

	public static bool TryDequeue<T>(this Queue<T> queue, out T element)
	{
		if (queue.Count > 0)
		{
			element = queue.Dequeue();
			return true;
		}
		element = default(T);
		return false;
	}

	public static T[] ToArray<T>(this Array array)
	{
		T[] array2 = new T[array.Length];
		array.CopyTo(array2, 0);
		return array2;
	}

	public static int IndexOf(this GameObject[] array, GameObject obj)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == obj)
			{
				return i;
			}
		}
		return -1;
	}

	public static int IndexOf(this List<GameObject> list, GameObject obj)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == obj)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool Contains(this GameObject[] array, GameObject obj)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == obj)
			{
				return true;
			}
		}
		return false;
	}

	public static bool Contains(this List<GameObject> list, GameObject obj)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == obj)
			{
				return true;
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T FirstElement<T>(this ArraySegment<T> segment)
	{
		return segment.Array[segment.Offset];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T At<T>(this ArraySegment<T> segment, int index)
	{
		return segment.Array[segment.Offset + index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySegment<T> Segment<T>(this ArraySegment<T> segment, int offset)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset + offset, segment.Count - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySegment<T> Segment<T>(this ArraySegment<T> segment, int offset, int length)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset + offset, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySegment<T> Segment<T>(this T[] array, int offset)
	{
		return new ArraySegment<T>(array, offset, array.Length - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ArraySegment<T> Segment<T>(this T[] array, int offset, int length)
	{
		return new ArraySegment<T>(array, offset, length);
	}

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory) where TValue : class
	{
		TValue tvalue;
		if (!dictionary.TryGetValue(key, out tvalue))
		{
			return dictionary[key] = factory();
		}
		return tvalue;
	}
}
