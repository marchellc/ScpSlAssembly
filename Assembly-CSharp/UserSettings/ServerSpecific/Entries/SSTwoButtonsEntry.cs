using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSTwoButtonsEntry : UserSettingsTwoButtons, ISSEntry
{
	private SSTwoButtonsSetting _setting;

	[SerializeField]
	private TMP_Text _optionA;

	[SerializeField]
	private TMP_Text _optionB;

	[SerializeField]
	private SSEntryLabel _label;

	protected override void SaveValue(bool val)
	{
		PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);
		_setting.SyncIsB = val;
		_setting.ClientSendValue();
	}

	protected override bool ReadSavedValue()
	{
		_setting.SyncIsB = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultIsB);
		return _setting.SyncIsB;
	}

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSTwoButtonsSetting;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		_setting = setting as SSTwoButtonsSetting;
		_label.Set(_setting);
		_optionA.text = _setting.OptionA;
		_optionB.text = _setting.OptionB;
		Setup();
		UpdateColors(instant: true);
	}
}
