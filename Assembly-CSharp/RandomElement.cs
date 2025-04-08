using System;
using System.Collections.Generic;
using UnityEngine;

public static class RandomElement
{
	public static T RandomItem<T>(this T[] array)
	{
		return array[global::UnityEngine.Random.Range(0, array.Length)];
	}

	public static T RandomItem<T>(this List<T> list)
	{
		return list[global::UnityEngine.Random.Range(0, list.Count)];
	}

	public static T PullRandomItem<T>(this List<T> list)
	{
		int num = global::UnityEngine.Random.Range(0, list.Count);
		T t = list[num];
		list.RemoveAt(num);
		return t;
	}
}
