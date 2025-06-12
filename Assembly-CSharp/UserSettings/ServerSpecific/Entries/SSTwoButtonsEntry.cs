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
		PlayerPrefsSl.Set(this._setting.PlayerPrefsKey, val);
		this._setting.SyncIsB = val;
		this._setting.ClientSendValue();
	}

	protected override bool ReadSavedValue()
	{
		this._setting.SyncIsB = PlayerPrefsSl.Get(this._setting.PlayerPrefsKey, this._setting.DefaultIsB);
		return this._setting.SyncIsB;
	}

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSTwoButtonsSetting;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		this._setting = setting as SSTwoButtonsSetting;
		this._label.Set(this._setting);
		this._optionA.text = this._setting.OptionA;
		this._optionB.text = this._setting.OptionB;
		base.Setup();
		base.UpdateColors(instant: true);
	}
}
