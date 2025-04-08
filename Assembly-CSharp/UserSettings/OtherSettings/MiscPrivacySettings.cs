using System;
using UnityEngine;

namespace UserSettings.OtherSettings
{
	public static class MiscPrivacySettings
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			UserSetting<bool>.SetDefaultValue<MiscPrivacySetting>(MiscPrivacySetting.SteamLobbyPrivacy, true);
			UserSetting<bool>.SetDefaultValue<MiscPrivacySetting>(MiscPrivacySetting.RichPresence, true);
			UserSetting<int>.SetDefaultValue<MiscPrivacySetting>(MiscPrivacySetting.SteamLobbyPrivacy, 1);
		}
	}
}
