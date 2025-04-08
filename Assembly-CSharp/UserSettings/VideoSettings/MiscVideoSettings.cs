using System;
using UnityEngine;

namespace UserSettings.VideoSettings
{
	public static class MiscVideoSettings
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			UserSetting<bool>.SetDefaultValue<MiscVideoSetting>(MiscVideoSetting.ExplosionShake, true);
			UserSetting<bool>.SetDefaultValue<MiscVideoSetting>(MiscVideoSetting.HeadBobbing, true);
			UserSetting<bool>.SetDefaultValue<MiscVideoSetting>(MiscVideoSetting.ShowNeedles, true);
		}
	}
}
