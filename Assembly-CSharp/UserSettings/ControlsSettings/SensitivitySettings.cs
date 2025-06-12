using UnityEngine;

namespace UserSettings.ControlsSettings;

public static class SensitivitySettings
{
	private static CachedUserSetting<float> _sensMultiplier;

	private static CachedUserSetting<float> _adsReductionMultiplier;

	private static CachedUserSetting<bool> _invert;

	public static bool SmoothInput;

	public static float SensMultiplier
	{
		get
		{
			return SensitivitySettings._sensMultiplier.Value;
		}
		set
		{
			SensitivitySettings._sensMultiplier.Value = value;
		}
	}

	public static float AdsReductionMultiplier
	{
		get
		{
			return SensitivitySettings._adsReductionMultiplier.Value;
		}
		set
		{
			SensitivitySettings._adsReductionMultiplier.Value = value;
		}
	}

	public static bool Invert
	{
		get
		{
			return SensitivitySettings._invert.Value;
		}
		set
		{
			SensitivitySettings._invert.Value = value;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UserSetting<float>.SetDefaultValue(SensitivitySetting.SensMultiplier, 1f);
		UserSetting<float>.SetDefaultValue(SensitivitySetting.AdsReductionMultiplier, 1f);
		SensitivitySettings._sensMultiplier = new CachedUserSetting<float>(SensitivitySetting.SensMultiplier);
		SensitivitySettings._adsReductionMultiplier = new CachedUserSetting<float>(SensitivitySetting.AdsReductionMultiplier);
		SensitivitySettings._invert = new CachedUserSetting<bool>(SensitivitySetting.Invert);
	}
}
