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
		string arg = value.ToString(this._setting.ValueToStringFormat);
		this._inputField.SetTextWithoutNotify(string.Format(this._setting.FinalDisplayFormat, arg));
	}

	private void OnDisable()
	{
		if (this._setting != null && this._setting.SyncDragging)
		{
			this._setting.SyncDragging = false;
			this._setting.ClientSendValue();
		}
	}

	protected override void SaveValue(float val)
	{
		PlayerPrefsSl.Set(this._setting.PlayerPrefsKey, val);
		this._setting.SyncDragging = true;
		this._setting.SyncFloatValue = val;
		this._setting.ClientSendValue();
	}

	protected override float ReadSavedValue()
	{
		this._setting.SyncFloatValue = PlayerPrefsSl.Get(this._setting.PlayerPrefsKey, this._setting.DefaultValue);
		return this._setting.SyncFloatValue;
	}

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSSliderSetting;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		this._setting = setting as SSSliderSetting;
		this._label.Set(this._setting);
		base.TargetUI.minValue = this._setting.MinValue;
		base.TargetUI.maxValue = this._setting.MaxValue;
		base.TargetUI.wholeNumbers = this._setting.Integer;
		this._inputField.contentType = (this._setting.Integer ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber);
		this._inputField.onEndEdit.AddListener(delegate(string str)
		{
			if (!float.TryParse(str, out var result))
			{
				this.SetValueAndTriggerEvent(base.StoredValue);
			}
			else
			{
				EventSystem current = EventSystem.current;
				if (!current.alreadySelecting)
				{
					current.SetSelectedGameObject(null);
				}
				result = Mathf.Clamp(result, this._setting.MinValue, this._setting.MaxValue);
				this.SetValueAndTriggerEvent(result);
				this.UpdateFieldText(result);
			}
		});
		this._inputField.onSelect.AddListener(delegate
		{
			this._inputField.SetTextWithoutNotify(base.TargetUI.value.ToString());
		});
		base.TargetUI.onValueChanged.AddListener(UpdateFieldText);
		base.Setup();
		this.UpdateFieldText(base.TargetUI.value);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && this._setting.SyncDragging)
		{
			this._setting.SyncDragging = false;
			this._setting.ClientSendValue();
		}
	}
}
