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
		PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);
		_setting.SyncSelectionIndexRaw = val;
		_setting.ClientSendValue();
	}

	protected override int ReadSavedValue()
	{
		_setting.SyncSelectionIndexRaw = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultOptionIndex);
		return _setting.SyncSelectionIndexRaw;
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
		_setting = setting as SSDropdownSetting;
		_label.Set(_setting);
		base.TargetUI.options.Clear();
		string[] options = _setting.Options;
		foreach (string text in options)
		{
			base.TargetUI.options.Add(new TMP_Dropdown.OptionData(text));
		}
		base.TargetUI.RefreshShownValue();
		Setup();
	}
}
