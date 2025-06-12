using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;

namespace MapGeneration.Decoration;

public class EnvironmentalAlarm : MonoBehaviour
{
	private static readonly int EmissiveColorPropertyID = Shader.PropertyToID("_EmissiveColor");

	private static readonly float AnimationSpeed = 1.15f;

	private static readonly AnimationCurve EaseInOutCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f, 0f, 0f));

	private static readonly AnimationCurve EaseInCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f, 0f, 0f));

	[SerializeField]
	private Light[] _lights;

	[SerializeField]
	private AudioSource[] _audioSources;

	[SerializeField]
	private MeshRenderer[] _emissiveMatMeshRenderers;

	[SerializeField]
	private AutoMoveAndRotate _lightRotator;

	[SerializeField]
	[HideInInspector]
	private List<Color> _enabledEmissionColors = new List<Color>();

	private float[] _defaultLightIntensities;

	private Material[] _materialCopies;

	private float _animationProgress;

	[field: SerializeField]
	public bool IsEnabled { get; set; }

	private bool IsAnimationComplete
	{
		get
		{
			if (this.IsEnabled && this._animationProgress >= 1f)
			{
				return true;
			}
			if (!this.IsEnabled && this._animationProgress <= 0f)
			{
				return true;
			}
			return false;
		}
	}

	private void Start()
	{
		this._defaultLightIntensities = new float[this._lights.Length];
		this._materialCopies = new Material[this._emissiveMatMeshRenderers.Length];
		for (int i = 0; i < this._lights.Length; i++)
		{
			this._defaultLightIntensities[i] = this._lights[i].intensity;
		}
		for (int j = 0; j < this._emissiveMatMeshRenderers.Length; j++)
		{
			Material material = new Material(this._emissiveMatMeshRenderers[j].sharedMaterial);
			this._materialCopies[j] = material;
			this._emissiveMatMeshRenderers[j].sharedMaterial = material;
		}
		this.Animate();
	}

	private void Update()
	{
		if (!this.IsAnimationComplete)
		{
			this.Animate();
		}
	}

	private void Animate()
	{
		float num = Time.deltaTime * EnvironmentalAlarm.AnimationSpeed;
		float num2 = (this.IsEnabled ? num : (0f - num));
		this._animationProgress = Mathf.Clamp01(this._animationProgress + num2);
		float num3 = EnvironmentalAlarm.EaseInOutCurve.Evaluate(this._animationProgress);
		float num4 = EnvironmentalAlarm.EaseInCurve.Evaluate(this._animationProgress);
		for (int i = 0; i < this._defaultLightIntensities.Length; i++)
		{
			this._lights[i].intensity = this._defaultLightIntensities[i] * num3;
		}
		for (int j = 0; j < this._materialCopies.Length; j++)
		{
			Color value = Color.Lerp(Color.black, this._enabledEmissionColors[j], num3);
			this._emissiveMatMeshRenderers[j].material.SetColor(EnvironmentalAlarm.EmissiveColorPropertyID, value);
		}
		AudioSource[] audioSources = this._audioSources;
		foreach (AudioSource audioSource in audioSources)
		{
			audioSource.volume = (this.IsEnabled ? 1f : num4);
			if (audioSource.volume == 0f && audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			if (audioSource.volume > 0f && !audioSource.isPlaying)
			{
				audioSource.Play();
			}
		}
		this._lightRotator.Multiplier = num4;
	}

	private void OnValidate()
	{
		this._enabledEmissionColors.Clear();
		MeshRenderer[] emissiveMatMeshRenderers = this._emissiveMatMeshRenderers;
		foreach (MeshRenderer meshRenderer in emissiveMatMeshRenderers)
		{
			if (!(meshRenderer.sharedMaterial == null))
			{
				Color color = meshRenderer.sharedMaterial.GetColor(EnvironmentalAlarm.EmissiveColorPropertyID);
				this._enabledEmissionColors.Add(color);
			}
		}
	}

	private void OnDestroy()
	{
		Material[] materialCopies = this._materialCopies;
		for (int i = 0; i < materialCopies.Length; i++)
		{
			Object.Destroy(materialCopies[i]);
		}
	}
}
