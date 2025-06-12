using UnityEngine;

namespace UserSettings.VideoSettings;

public static class PerformanceVideoSettings
{
	private static readonly int[] TextureQualityPresets = new int[4] { 3, 2, 1, 0 };

	private static void ApplyTextureResolution(int presetIndex)
	{
		QualitySettings.globalTextureMipmapLimit = PerformanceVideoSettings.TextureQualityPresets[Mathf.Clamp(presetIndex, 0, PerformanceVideoSettings.TextureQualityPresets.Length - 1)];
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PerformanceVideoSettings.SetDefaultValues();
		UserSetting<int>.AddListener(PerformanceVideoSetting.TextureResolution, ApplyTextureResolution);
		PerformanceVideoSettings.ApplyAll();
	}

	private static void SetDefaultValues()
	{
		UserSetting<int>.SetDefaultValue(PerformanceVideoSetting.AntiAliasingType, 1);
		UserSetting<int>.SetDefaultValue(PerformanceVideoSetting.AntiAliasingQuality, 1);
		UserSetting<float>.SetDefaultValue(PerformanceVideoSetting.RagdollFreeze, 30f);
		UserSetting<int>.SetDefaultValue(PerformanceVideoSetting.BloomQuality, 1);
		UserSetting<int>.SetDefaultValue(PerformanceVideoSetting.AOQuality, 2);
		UserSetting<int>.SetDefaultValue(PerformanceVideoSetting.TextureResolution, 3);
		UserSetting<bool>.SetDefaultValue(PerformanceVideoSetting.BloodDecalsEnabled, defaultValue: true);
		UserSetting<bool>.SetDefaultValue(PerformanceVideoSetting.BulletDecalsEnabled, defaultValue: true);
		UserSetting<float>.SetDefaultValue(PerformanceVideoSetting.BloodDecalsLimit, 1000f);
		UserSetting<float>.SetDefaultValue(PerformanceVideoSetting.BulletDecalsLimits, 3000f);
	}

	private static void ApplyAll()
	{
		PerformanceVideoSettings.ApplyTextureResolution(UserSetting<int>.Get(PerformanceVideoSetting.TextureResolution));
	}
}
