using System;
using UnityEngine;

namespace UserSettings.VideoSettings
{
	public static class PerformanceVideoSettings
	{
		private static void ApplyTextureResolution(int presetIndex)
		{
			QualitySettings.masterTextureLimit = PerformanceVideoSettings.TextureQualityPresets[Mathf.Clamp(presetIndex, 0, PerformanceVideoSettings.TextureQualityPresets.Length - 1)];
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PerformanceVideoSettings.SetDefaultValues();
			UserSetting<int>.AddListener<PerformanceVideoSetting>(PerformanceVideoSetting.TextureResolution, new Action<int>(PerformanceVideoSettings.ApplyTextureResolution));
			PerformanceVideoSettings.ApplyAll();
		}

		private static void SetDefaultValues()
		{
			UserSetting<int>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.AntiAliasingType, 1);
			UserSetting<int>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.AntiAliasingQuality, 1);
			UserSetting<float>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.RagdollFreeze, 30f);
			UserSetting<int>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.BloomQuality, 1);
			UserSetting<int>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.AOQuality, 2);
			UserSetting<int>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.TextureResolution, 3);
			UserSetting<bool>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.BloodDecalsEnabled, true);
			UserSetting<bool>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.BulletDecalsEnabled, true);
			UserSetting<float>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.BloodDecalsLimit, 1000f);
			UserSetting<float>.SetDefaultValue<PerformanceVideoSetting>(PerformanceVideoSetting.BulletDecalsLimits, 3000f);
		}

		private static void ApplyAll()
		{
			PerformanceVideoSettings.ApplyTextureResolution(UserSetting<int>.Get<PerformanceVideoSetting>(PerformanceVideoSetting.TextureResolution));
		}

		private static readonly int[] TextureQualityPresets = new int[] { 3, 2, 1, 0 };
	}
}
