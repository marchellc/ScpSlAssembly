using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSDropdownEntry : UserSettingsDropdown, ISSEntry
{
	private SSDropdownSetting _setting;

	[SerializeField]
	private SSEntryLabel _label;

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
		if (setting is SSDropdownSetting sSDropdownSetting)
		{
			return sSDropdownSetting.EntryType == SSDropdownSetting.DropdownEntryType.Regular;
		}
		return false;
	}

	public virtual void Init(ServerSpecificSettingBase setting)
	{
		this._setting = setting as SSDropdownSetting;
		this._label.Set(this._setting);
		base.TargetUI.options.Clear();
		string[] options = this._setting.Options;
		foreach (string text in options)
		{
			base.TargetUI.options.Add(new TMP_Dropdown.OptionData(text));
		}
		base.TargetUI.RefreshShownValue();
		base.Setup();
	}
}
