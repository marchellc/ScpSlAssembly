using UnityEngine;

namespace UserSettings.AudioSettings;

public static class VcAudioSettings
{
	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UserSetting<bool>.SetDefaultValue(VcAudioSetting.NoiseReduction, defaultValue: true);
	}
}
