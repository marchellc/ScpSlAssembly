using System;
using UnityEngine;

public class WorldspaceLightParticleConverter : MonoBehaviour
{
	private static Material InvisibleMaterial
	{
		get
		{
			if (!WorldspaceLightParticleConverter._matLoaded)
			{
				WorldspaceLightParticleConverter._invisibleMat = Resources.Load<Material>("StaticMaterials/Invisible");
				WorldspaceLightParticleConverter._matLoaded = true;
			}
			return WorldspaceLightParticleConverter._invisibleMat;
		}
	}

	public static void Convert(ParticleSystem system, bool includeSubsystems)
	{
		if (includeSubsystems)
		{
			system.gameObject.ForEachComponentInChildren(new Action<ParticleSystem>(WorldspaceLightParticleConverter.ConvertIndividual), true);
			return;
		}
		WorldspaceLightParticleConverter.ConvertIndividual(system);
	}

	public static void Convert(ParticleSystem system)
	{
		WorldspaceLightParticleConverter.Convert(system, true);
	}

	private static void ConvertIndividual(ParticleSystem system)
	{
		if (!system.lights.enabled || system.lights.light == null)
		{
			return;
		}
		Transform transform = system.transform;
		ParticleSystem particleSystem = global::UnityEngine.Object.Instantiate<ParticleSystem>(system, transform);
		Transform transform2 = particleSystem.transform;
		ParticleSystem.MainModule main = particleSystem.main;
		transform2.localPosition = Vector3.zero;
		transform2.localRotation = Quaternion.identity;
		ParticleSystemScalingMode scalingMode = main.scalingMode;
		if (scalingMode != ParticleSystemScalingMode.Hierarchy)
		{
			if (scalingMode == ParticleSystemScalingMode.Local)
			{
				transform2.localScale = transform.localScale;
			}
		}
		else
		{
			transform2.localScale = Vector3.one;
		}
		particleSystem.gameObject.layer = 0;
		particleSystem.GetComponent<ParticleSystemRenderer>().sharedMaterial = WorldspaceLightParticleConverter.InvisibleMaterial;
		main.duration = Mathf.Max(main.duration, 0.025f);
		Transform transform3 = particleSystem.lights.light.transform;
		for (int i = 0; i < transform2.childCount; i++)
		{
			Transform child = transform2.GetChild(i);
			if (!(child == transform3))
			{
				global::UnityEngine.Object.Destroy(child.gameObject);
			}
		}
	}

	private const string MaterialPath = "StaticMaterials/Invisible";

	private const float MinFlashDur = 0.025f;

	private static Material _invisibleMat;

	private static bool _matLoaded;
}
