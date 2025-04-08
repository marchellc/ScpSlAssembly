using System;

namespace InventorySystem.Items.Firearms.Attachments
{
	public static class AttachmentPreferences
	{
		private static string PreferencesPath(ItemType weaponId)
		{
			int num = (int)weaponId;
			return num.ToString() + "_AttachmentsSetupPreference_" + AttachmentPreferences.GetPreset(weaponId).ToString();
		}

		public static uint GetPreferenceCodeOfPreset(ItemType weapon, int preset)
		{
			int num = (int)weapon;
			return PlayerPrefsSl.Get(num.ToString() + "_AttachmentsSetupPreference_" + preset.ToString(), 0U);
		}

		public static int GetPreset(ItemType weapon)
		{
			string text = "Preset_";
			int num = (int)weapon;
			return PlayerPrefsSl.Get(text + num.ToString(), 0);
		}

		public static void SetPreset(ItemType weapon, int presetId)
		{
			string text = "Preset_";
			int num = (int)weapon;
			PlayerPrefsSl.Set(text + num.ToString(), presetId);
		}

		public static uint GetSavedPreferenceCode(ItemType weapon)
		{
			return (InventoryItemLoader.AvailableItems[weapon] as Firearm).GetSavedPreferenceCode();
		}

		public static uint GetSavedPreferenceCode(this Firearm weapon)
		{
			return PlayerPrefsSl.Get(AttachmentPreferences.PreferencesPath(weapon.ItemTypeId), weapon.ValidateAttachmentsCode(0U));
		}

		public static void SavePreferenceCode(ItemType weapon, uint code)
		{
			PlayerPrefsSl.Set(AttachmentPreferences.PreferencesPath(weapon), code);
		}

		public static void SavePreferenceCode(this Firearm weapon)
		{
			AttachmentPreferences.SavePreferenceCode(weapon.ItemTypeId, weapon.GetCurrentAttachmentsCode());
		}

		private const string PrefKey = "_AttachmentsSetupPreference_";

		private const string PresetKey = "Preset_";
	}
}
