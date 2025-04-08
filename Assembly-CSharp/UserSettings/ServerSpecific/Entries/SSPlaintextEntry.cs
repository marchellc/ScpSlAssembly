using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSPlaintextEntry : UserSettingsUIBase<TMP_InputField, string>, ISSEntry
	{
		protected override UnityEvent<string> OnValueChangedEvent
		{
			get
			{
				return base.TargetUI.onEndEdit;
			}
		}

		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSPlaintextSetting;
		}

		public void Init(ServerSpecificSettingBase setting)
		{
			this._setting = setting as SSPlaintextSetting;
			this._setting.OnClearRequested += this.ClearField;
			this._label.Set(this._setting);
			this._placeholder.text = this._setting.Placeholder;
			this._inputField.contentType = this._setting.ContentType;
			this._inputField.characterLimit = this._setting.CharacterLimit;
			base.Setup();
		}

		protected override void Awake()
		{
			base.Awake();
			base.TargetUI.onEndEdit.AddListener(delegate(string _)
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
			PlayerPrefsSl.Set(this._setting.PlayerPrefsKey, val);
			this._setting.SyncInputText = val;
			this._setting.ClientSendValue();
		}

		protected override string ReadSavedValue()
		{
			this._setting.SyncInputText = PlayerPrefsSl.Get(this._setting.PlayerPrefsKey, string.Empty);
			return this._setting.SyncInputText;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (this._setting != null)
			{
				this._setting.OnClearRequested -= this.ClearField;
			}
		}

		protected override void SetValueAndTriggerEvent(string val)
		{
			this._inputField.text = val;
		}

		protected override void SetValueWithoutNotify(string val)
		{
			this._inputField.SetTextWithoutNotify(val);
		}

		private void ClearField()
		{
			if (this._inputField.isFocused)
			{
				return;
			}
			this.SetValueAndTriggerEvent(string.Empty);
		}

		private SSPlaintextSetting _setting;

		[SerializeField]
		private TMP_InputField _inputField;

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_Text _placeholder;
	}
}
