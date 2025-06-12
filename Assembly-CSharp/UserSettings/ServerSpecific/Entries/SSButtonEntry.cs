using TMPro;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries;

public class SSButtonEntry : HoldableButton, ISSEntry
{
	[SerializeField]
	private SSEntryLabel _label;

	[SerializeField]
	private TMP_Text _buttonText;

	private float _holdTime;

	private SSButton _setting;

	public override float HoldTime => this._holdTime;

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSButton;
	}

	public void Init(ServerSpecificSettingBase settingBase)
	{
		this._setting = settingBase as SSButton;
		this._label.Set(this._setting);
		this._holdTime = this._setting.HoldTimeSeconds;
		if (this._holdTime > 0f)
		{
			base.OnHeld.AddListener(OnTrigger);
		}
		else
		{
			base.onClick.AddListener(OnTrigger);
		}
		this._buttonText.text = this._setting.ButtonText;
	}

	private void OnTrigger()
	{
		this._setting.ClientSendValue();
	}
}
