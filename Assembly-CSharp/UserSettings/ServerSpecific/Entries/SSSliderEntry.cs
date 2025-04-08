using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSSliderEntry : UserSettingsSlider, ISSEntry, IPointerUpHandler, IEventSystemHandler
	{
		private void UpdateFieldText(float value)
		{
			string text = value.ToString(this._setting.ValueToStringFormat);
			this._inputField.SetTextWithoutNotify(string.Format(this._setting.FinalDisplayFormat, text));
		}

		private void OnDisable()
		{
			if (this._setting == null || !this._setting.SyncDragging)
			{
				return;
			}
			this._setting.SyncDragging = false;
			this._setting.ClientSendValue();
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
				float num;
				if (!float.TryParse(str, out num))
				{
					this.SetValueAndTriggerEvent(base.StoredValue);
					return;
				}
				EventSystem current = EventSystem.current;
				if (!current.alreadySelecting)
				{
					current.SetSelectedGameObject(null);
				}
				num = Mathf.Clamp(num, this._setting.MinValue, this._setting.MaxValue);
				this.SetValueAndTriggerEvent(num);
				this.UpdateFieldText(num);
			});
			this._inputField.onSelect.AddListener(delegate(string _)
			{
				this._inputField.SetTextWithoutNotify(base.TargetUI.value.ToString());
			});
			base.TargetUI.onValueChanged.AddListener(new UnityAction<float>(this.UpdateFieldText));
			base.Setup();
			this.UpdateFieldText(base.TargetUI.value);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}
			if (!this._setting.SyncDragging)
			{
				return;
			}
			this._setting.SyncDragging = false;
			this._setting.ClientSendValue();
		}

		private SSSliderSetting _setting;

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_InputField _inputField;
	}
}
