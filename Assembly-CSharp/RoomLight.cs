using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProgressiveCulling;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UserSettings;
using UserSettings.VideoSettings;

public class RoomLight : CullableBehaviour, IBoundsCullable, ICullable
{
	public static readonly Color DefaultWarheadColor = new Color(1f, 0.2f, 0.2f);

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissiveColor");

	private static readonly int BlackoutProperty = Shader.PropertyToID("_BlackoutEmissionMultiplier");

	private static readonly CachedUserSetting<bool> FacilityLightsSetting = new CachedUserSetting<bool>(LightingVideoSetting.RenderLights);

	private static readonly HashSet<RoomLight> RenderedInstances = new HashSet<RoomLight>();

	[SerializeField]
	private int _materialId;

	[SerializeField]
	private Renderer[] _renderers;

	[Tooltip("Only used if no light source is detected. Does not need to be accurate, default should be sufficent unless you see objects culling while visible.")]
	[SerializeField]
	private float _lightSize = 3f;

	private Material _copy;

	private Transform _centerTr;

	private int _rendererCount;

	private bool _useEmissionMatProperty;

	private bool _useBlackoutMatProperty;

	private bool _isDirty;

	private bool _cacheSet;

	private bool _warheadInProgress;

	private bool _overrideColorSet;

	private bool _preventSubCulling;

	private bool _targetBlackout;

	private readonly Stopwatch _blackoutAnimProgress = new Stopwatch();

	private Color _overrideColor;

	private Color _initialLightColor;

	private Color _initialMaterialColor;

	internal bool HasLight;

	internal Light LightSource { get; private set; }

	public override bool ShouldBeVisible
	{
		get
		{
			if (!this._preventSubCulling)
			{
				return CullingCamera.CheckBoundsVisibility(this.WorldspaceBounds);
			}
			return true;
		}
	}

	public Bounds WorldspaceBounds
	{
		get
		{
			this.UpdateCache();
			return new Bounds(this._centerTr.position, Vector3.one * this._lightSize);
		}
	}

	private void UpdateCache()
	{
		if (this._cacheSet)
		{
			return;
		}
		this._cacheSet = true;
		this._rendererCount = this._renderers.Length;
		this.LightSource = base.GetComponentInChildren<Light>();
		this.HasLight = this.LightSource != null;
		if (!this.HasLight)
		{
			this._centerTr = base.transform;
			return;
		}
		this._centerTr = this.LightSource.transform;
		this._initialLightColor = this.LightSource.color;
		this._lightSize = this.LightSource.range * 2f;
		if (this.LightSource.TryGetComponent<HDAdditionalLightData>(out var component))
		{
			HDLightType type = component.type;
			if (type == HDLightType.Directional || type == HDLightType.Area)
			{
				this._preventSubCulling = true;
			}
		}
	}

	private void OnValidate()
	{
		this._cacheSet = false;
	}

	private void Awake()
	{
		this.UpdateCache();
		if (this._centerTr.TryGetComponentInParent<CullableRoom>(out var comp))
		{
			comp.AddChildCullable(this);
		}
		if (this._rendererCount > 0)
		{
			this._copy = new Material(this._renderers[0].sharedMaterials[this._materialId]);
			for (int i = 0; i < this._rendererCount; i++)
			{
				Material[] sharedMaterials = this._renderers[i].sharedMaterials;
				sharedMaterials[this._materialId] = this._copy;
				this._renderers[i].materials = sharedMaterials;
			}
			if (this._copy.HasProperty(RoomLight.EmissionColor))
			{
				this._useEmissionMatProperty = true;
				this._initialMaterialColor = this._copy.GetColor(RoomLight.EmissionColor);
			}
			if (this._copy.HasProperty(RoomLight.BlackoutProperty))
			{
				this._useBlackoutMatProperty = true;
			}
			this.UpdateWarhead(AlphaWarheadController.InProgress);
		}
	}

	protected override void UpdateVisible()
	{
		base.UpdateVisible();
		if (this._isDirty)
		{
			this.UpdateIntensity();
		}
	}

	private void OnDestroy()
	{
		RoomLight.RenderedInstances.Remove(this);
	}

	private void MarkAsDirty()
	{
		this._isDirty = true;
		this.UpdateIntensity();
	}

	private void UpdateWarhead(bool val)
	{
		this._warheadInProgress = val;
		this.MarkAsDirty();
	}

	private void UpdateIntensity()
	{
		Color matColor;
		Color lightColor;
		if (this._overrideColorSet)
		{
			matColor = this._overrideColor;
			lightColor = this._overrideColor;
		}
		else
		{
			matColor = (this._warheadInProgress ? RoomLight.DefaultWarheadColor : this._initialMaterialColor);
			lightColor = (this._warheadInProgress ? RoomLight.DefaultWarheadColor : this._initialLightColor);
		}
		if (!this._blackoutAnimProgress.IsRunning)
		{
			this._isDirty = false;
			this.SetColors(lightColor, matColor, (!this._targetBlackout) ? 1 : 0);
			return;
		}
		float num = Mathf.Clamp01((float)this._blackoutAnimProgress.Elapsed.TotalSeconds);
		float intensity = (this._targetBlackout ? this.FlickerBlackout(num) : this.FlickerReEnabling(num));
		this.SetColors(lightColor, matColor, intensity);
		if (!(num < 1f))
		{
			this._blackoutAnimProgress.Stop();
		}
	}

	private float FlickerBlackout(float f)
	{
		float num = f * f * f * f;
		return Mathf.Sin(MathF.PI * f * 20f) * (1f - num);
	}

	private float FlickerReEnabling(float f)
	{
		if (Mathf.Approximately(f, 0f))
		{
			return 0f;
		}
		float num = f * f * f;
		return f / (Mathf.Cos(MathF.PI / num) + 2f);
	}

	private void SetColors(Color lightColor, Color matColor, float intensity)
	{
		if (this._rendererCount > 0)
		{
			if (this._useEmissionMatProperty)
			{
				this._copy.SetColor(RoomLight.EmissionColor, matColor * intensity);
			}
			if (this._useBlackoutMatProperty)
			{
				this._copy.SetFloat(RoomLight.BlackoutProperty, intensity);
			}
		}
		if (this.HasLight)
		{
			this.LightSource.color = lightColor * intensity;
		}
	}

	internal void SetOverrideColor(Color overrideColor)
	{
		this._overrideColorSet = true;
		this._overrideColor = overrideColor;
		this.MarkAsDirty();
	}

	internal void ResetOverrideColor()
	{
		this._overrideColorSet = false;
		this.MarkAsDirty();
	}

	internal void SetBlackout(bool state)
	{
		this._targetBlackout = state;
		this._blackoutAnimProgress.Restart();
		this.MarkAsDirty();
	}

	protected override void OnVisibilityChanged(bool isVisible)
	{
		base.gameObject.SetActive(isVisible && RoomLight.FacilityLightsSetting.Value);
		if (isVisible)
		{
			this.UpdateWarhead(AlphaWarheadController.InProgress);
			RoomLight.RenderedInstances.Add(this);
		}
		else
		{
			RoomLight.RenderedInstances.Remove(this);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		AlphaWarheadController.OnProgressChanged += OnWarheadChanged;
		UserSetting<bool>.AddListener(LightingVideoSetting.RenderLights, OnRenderLightsSettingChanged);
	}

	private static void OnRenderLightsSettingChanged(bool newSetting)
	{
		foreach (RoomLight renderedInstance in RoomLight.RenderedInstances)
		{
			if (!renderedInstance.IsCulled)
			{
				renderedInstance.gameObject.SetActive(newSetting);
			}
		}
	}

	private static void OnWarheadChanged(bool newProgress)
	{
		foreach (RoomLight renderedInstance in RoomLight.RenderedInstances)
		{
			renderedInstance.UpdateWarhead(newProgress);
		}
	}
}
