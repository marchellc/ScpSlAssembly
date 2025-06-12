using System;
using MapGeneration;
using MapGeneration.StaticHelpers;
using UnityEngine;
using UnityStandardAssets.Utility;

public class StaticBaker : MonoBehaviour
{
	private static readonly Type[] ExtractedTypes = new Type[3]
	{
		typeof(Animator),
		typeof(AutoMoveAndRotate),
		typeof(IBlockStaticBatching)
	};

	private static GameObject _currentRoot;

	private void Awake()
	{
		StaticBaker._currentRoot = base.gameObject;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationStage += OnGenerationStage;
	}

	private static void OnGenerationStage(MapGenerationPhase stage)
	{
		if (stage == MapGenerationPhase.StaticBatching)
		{
			GameObject currentRoot = StaticBaker._currentRoot;
			Type[] extractedTypes = StaticBaker.ExtractedTypes;
			foreach (Type type in extractedTypes)
			{
				StaticBaker.ExtractType(currentRoot, type);
			}
			StaticBatchingUtility.Combine(currentRoot);
		}
	}

	private static void ExtractType(GameObject rootObject, Type type)
	{
		Component[] componentsInChildren = rootObject.GetComponentsInChildren(type, includeInactive: true);
		foreach (Component component in componentsInChildren)
		{
			if (StaticBaker.ShouldExtract(component))
			{
				component.transform.parent.gameObject.AddComponent<DynamicChildController>().SetChild(component.gameObject);
			}
		}
	}

	private static bool ShouldExtract(Component component)
	{
		if (component.GetComponentInParent<Canvas>(includeInactive: true) != null)
		{
			return false;
		}
		Type[] extractedTypes = StaticBaker.ExtractedTypes;
		foreach (Type t in extractedTypes)
		{
			if (component.transform.parent.GetComponentInParent(t, includeInactive: true) != null)
			{
				return false;
			}
		}
		return true;
	}
}
