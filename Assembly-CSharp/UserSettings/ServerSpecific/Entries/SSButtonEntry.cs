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

	public override float HoldTime => _holdTime;

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSButton;
	}

	public void Init(ServerSpecificSettingBase settingBase)
	{
		_setting = settingBase as SSButton;
		_label.Set(_setting);
		_holdTime = _setting.HoldTimeSeconds;
		if (_holdTime > 0f)
		{
			base.OnHeld.AddListener(OnTrigger);
		}
		else
		{
			base.onClick.AddListener(OnTrigger);
		}
		_buttonText.text = _setting.ButtonText;
	}

	private void OnTrigger()
	{
		_setting.ClientSendValue();
	}
}
