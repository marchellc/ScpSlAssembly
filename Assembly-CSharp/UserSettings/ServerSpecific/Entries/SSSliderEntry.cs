using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSSliderEntry : UserSettingsSlider, ISSEntry, IPointerUpHandler, IEventSystemHandler
{
	private SSSliderSetting _setting;

	[SerializeField]
	private SSEntryLabel _label;

	[SerializeField]
	private TMP_InputField _inputField;

	private void UpdateFieldText(float value)
	{
		string arg = value.ToString(_setting.ValueToStringFormat);
		_inputField.SetTextWithoutNotify(string.Format(_setting.FinalDisplayFormat, arg));
	}

	private void OnDisable()
	{
		if (_setting != null && _setting.SyncDragging)
		{
			_setting.SyncDragging = false;
			_setting.ClientSendValue();
		}
	}

	protected override void SaveValue(float val)
	{
		PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);
		_setting.SyncDragging = true;
		_setting.SyncFloatValue = val;
		_setting.ClientSendValue();
	}

	protected override float ReadSavedValue()
	{
		_setting.SyncFloatValue = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultValue);
		return _setting.SyncFloatValue;
	}

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSSliderSetting;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		_setting = setting as SSSliderSetting;
		_label.Set(_setting);
		base.TargetUI.minValue = _setting.MinValue;
		base.TargetUI.maxValue = _setting.MaxValue;
		base.TargetUI.wholeNumbers = _setting.Integer;
		_inputField.contentType = (_setting.Integer ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber);
		_inputField.onEndEdit.AddListener(delegate(string str)
		{
			if (!float.TryParse(str, out var result))
			{
				SetValueAndTriggerEvent(base.StoredValue);
			}
			else
			{
				EventSystem current = EventSystem.current;
				if (!current.alreadySelecting)
				{
					current.SetSelectedGameObject(null);
				}
				result = Mathf.Clamp(result, _setting.MinValue, _setting.MaxValue);
				SetValueAndTriggerEvent(result);
				UpdateFieldText(result);
			}
		});
		_inputField.onSelect.AddListener(delegate
		{
			_inputField.SetTextWithoutNotify(base.TargetUI.value.ToString());
		});
		base.TargetUI.onValueChanged.AddListener(UpdateFieldText);
		Setup();
		UpdateFieldText(base.TargetUI.value);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && _setting.SyncDragging)
		{
			_setting.SyncDragging = false;
			_setting.ClientSendValue();
		}
	}
}
