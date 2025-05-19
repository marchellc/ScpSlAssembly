using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSPlaintextEntry : UserSettingsUIBase<TMP_InputField, string>, ISSEntry
{
	private SSPlaintextSetting _setting;

	[SerializeField]
	private TMP_InputField _inputField;

	[SerializeField]
	private SSEntryLabel _label;

	[SerializeField]
	private TMP_Text _placeholder;

	protected override UnityEvent<string> OnValueChangedEvent => base.TargetUI.onEndEdit;

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSPlaintextSetting;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		_setting = setting as SSPlaintextSetting;
		_setting.OnClearRequested += ClearField;
		_label.Set(_setting);
		_placeholder.text = _setting.Placeholder;
		_inputField.contentType = _setting.ContentType;
		_inputField.characterLimit = _setting.CharacterLimit;
		Setup();
	}

	protected override void Awake()
	{
		base.Awake();
		base.TargetUI.onEndEdit.AddListener(delegate
		{
			EventSystem current = EventSystem.current;
			if (!current.alreadySelecting)
			{
				current.SetSelectedGameObject(null);
			}
		});
	}

	protected override void SaveValue(string val)
	{
		PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);
		_setting.SyncInputText = val;
		_setting.ClientSendValue();
	}

	protected override string ReadSavedValue()
	{
		_setting.SyncInputText = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, string.Empty);
		return _setting.SyncInputText;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (_setting != null)
		{
			_setting.OnClearRequested -= ClearField;
		}
	}

	protected override void SetValueAndTriggerEvent(string val)
	{
		_inputField.text = val;
	}

	protected override void SetValueWithoutNotify(string val)
	{
		_inputField.SetTextWithoutNotify(val);
	}

	private void ClearField()
	{
		if (!_inputField.isFocused)
		{
			SetValueAndTriggerEvent(string.Empty);
		}
	}
}
