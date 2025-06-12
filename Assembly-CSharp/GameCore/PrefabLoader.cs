using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore;

public class PrefabLoader : MonoBehaviour
{
	[SerializeField]
	private List<UnityEngine.Object> _globalPrefabs;

	public static List<UnityEngine.Object> Prefabs { get; private set; } = new List<UnityEngine.Object>();

	public static bool TryFetch<T>(out T prefab, bool enableException = false) where T : UnityEngine.Object
	{
		foreach (UnityEngine.Object prefab2 in PrefabLoader.Prefabs)
		{
			if (prefab2 is T val)
			{
				prefab = val;
				return val;
			}
		}
		if (enableException)
		{
			throw new NullReferenceException("Couldn't find " + typeof(T).FullName + " in the registered prefabs.");
		}
		prefab = null;
		return false;
	}

	private void Awake()
	{
		PrefabLoader.Prefabs = this._globalPrefabs;
	}
}
