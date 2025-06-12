using System;
using System.Collections.Generic;
using System.Linq;
using MapGeneration.Holidays;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CustomRendering;

public class FogController : MonoBehaviour
{
	private static readonly int HashGlobalFogColor = Shader.PropertyToID("_GlobalFogColor");

	private static readonly int HashGlobalFogDistance = Shader.PropertyToID("_GlobalFogDistance");

	private static readonly Dictionary<HolidayType, FogType> HolidayDefaultFog = new Dictionary<HolidayType, FogType>
	{
		[HolidayType.None] = FogType.Inside,
		[HolidayType.Christmas] = FogType.ChristmasInside,
		[HolidayType.Halloween] = FogType.HalloweenInside
	};

	private FogSetting[] _fogSettings;

	private FogType? _fogType;

	public static float FogFarPlaneDistance { get; private set; }

	public static FogController Singleton { get; private set; }

	public static FogType DefaultFog
	{
		get
		{
			HolidayType activeHoliday = HolidayUtils.GetActiveHoliday();
			if (!FogController.HolidayDefaultFog.TryGetValue(activeHoliday, out var value))
			{
				return FogType.Inside;
			}
			return value;
		}
	}

	public CustomFog FogEffect { get; private set; }

	public FogType? ForcedFog
	{
		get
		{
			return this._fogType;
		}
		set
		{
			bool flag = !value.HasValue || value.Value != FogType.None;
			this.FogEffect.enabled = flag;
			this._fogType = value;
		}
	}

	public static void EnableFogType(FogType fogType, float seconds = 0f)
	{
		FogSetting fogSetting = FogController.Singleton.GetFogSetting(fogType);
		fogSetting.IsEnabled = true;
		fogSetting.BlendTime = seconds;
	}

	public static void DisableFogType(FogType fogType, float seconds = 0f)
	{
		FogSetting fogSetting = FogController.Singleton.GetFogSetting(fogType);
		fogSetting.IsEnabled = false;
		fogSetting.BlendTime = seconds;
	}

	public FogSetting GetFogSetting(FogType fogType)
	{
		FogSetting[] fogSettings = this._fogSettings;
		foreach (FogSetting fogSetting in fogSettings)
		{
			if (fogSetting.FogType == fogType)
			{
				return fogSetting;
			}
		}
		throw new NotImplementedException($"The FogSetting component for '{fogType}' needs to be attached to the FogController GameObject.");
	}

	private void Awake()
	{
		if (FogController.Singleton == null)
		{
			FogController.Singleton = this;
			this._fogSettings = (from x in base.GetComponents<FogSetting>()
				orderby x.Priority
				select x).ToArray();
			FogSetting[] fogSettings = this._fogSettings;
			foreach (FogSetting obj in fogSettings)
			{
				obj.enabled = obj.FogType == FogController.DefaultFog;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		this.FogEffect = (CustomFog)base.GetComponent<CustomPassVolume>().customPasses[0];
	}

	private void OnDisable()
	{
		Shader.SetGlobalVector(FogController.HashGlobalFogDistance, Vector2.one * float.MaxValue);
	}

	private void Update()
	{
		Color color = this._fogSettings[0].Color;
		float num = this._fogSettings[0].StartDistance;
		float num2 = this._fogSettings[0].EndDistance;
		for (int i = 1; i < this._fogSettings.Length; i++)
		{
			FogSetting fogSetting = this._fogSettings[i];
			if (this.ForcedFog.HasValue)
			{
				if (this.ForcedFog.Value != fogSetting.FogType)
				{
					continue;
				}
			}
			else if (!fogSetting.IsEnabled && fogSetting.Weight == 0f)
			{
				continue;
			}
			fogSetting.UpdateWeight();
			color = Color.Lerp(color, fogSetting.Color, fogSetting.Weight);
			num = Mathf.Lerp(num, fogSetting.StartDistance, fogSetting.Weight);
			num2 = Mathf.Lerp(num2, fogSetting.EndDistance, fogSetting.Weight);
		}
		this.FogEffect.FogColor = color;
		this.FogEffect.EndDistance = num2;
		this.FogEffect.StartDistance = num;
		Shader.SetGlobalVector(FogController.HashGlobalFogColor, color);
		Shader.SetGlobalVector(FogController.HashGlobalFogDistance, new Vector2(num, num2));
		FogController.FogFarPlaneDistance = num2;
	}
}
