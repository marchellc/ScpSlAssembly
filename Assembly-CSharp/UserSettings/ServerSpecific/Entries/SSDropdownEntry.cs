using System;
using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSDropdownEntry : UserSettingsDropdown, ISSEntry
	{
		protected override void SaveValue(int val)
		{
			PlayerPrefsSl.Set(this._setting.PlayerPrefsKey, val);
			this._setting.SyncSelectionIndexRaw = val;
			this._setting.ClientSendValue();
		}

		protected override int ReadSavedValue()
		{
			this._setting.SyncSelectionIndexRaw = PlayerPrefsSl.Get(this._setting.PlayerPrefsKey, this._setting.DefaultOptionIndex);
			return this._setting.SyncSelectionIndexRaw;
		}

		public virtual bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			SSDropdownSetting ssdropdownSetting = setting as SSDropdownSetting;
			return ssdropdownSetting != null && ssdropdownSetting.EntryType == SSDropdownSetting.DropdownEntryType.Regular;
		}

		public virtual void Init(ServerSpecificSettingBase setting)
		{
			this._setting = setting as SSDropdownSetting;
			this._label.Set(this._setting);
			base.TargetUI.options.Clear();
			foreach (string text in this._setting.Options)
			{
				base.TargetUI.options.Add(new TMP_Dropdown.OptionData(text));
			}
			base.TargetUI.RefreshShownValue();
			base.Setup();
		}

		private SSDropdownSetting _setting;

		[SerializeField]
		private SSEntryLabel _label;
	}
}
