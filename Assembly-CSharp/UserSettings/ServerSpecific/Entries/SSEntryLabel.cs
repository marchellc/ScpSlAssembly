using System;
using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

[Serializable]
public class SSEntryLabel
{
	[SerializeField]
	private TMP_Text _label;

	[SerializeField]
	private CustomUserSettingsEntryDescription _hint;

	public void Set(ServerSpecificSettingBase setting)
	{
		this._label.text = setting.Label;
		if (string.IsNullOrEmpty(setting.HintDescription))
		{
			this._hint.gameObject.SetActive(value: false);
			return;
		}
		this._hint.gameObject.SetActive(value: true);
		this._hint.SetCustomText(setting.HintDescription);
	}
}
