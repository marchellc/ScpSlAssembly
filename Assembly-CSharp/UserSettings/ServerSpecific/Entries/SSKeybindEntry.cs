using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSKeybindEntry : KeycodeField, ISSEntry
	{
		public void ApplySuggestion()
		{
			this.ApplyPressedKey(this._setting.SuggestedKey);
		}

		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSKeybindSetting;
		}

		public void Init(ServerSpecificSettingBase setting)
		{
			this._setting = setting as SSKeybindSetting;
			this._label.Set(setting);
			this._undoImage.GetComponent<Button>().onClick.AddListener(new UnityAction(this.PressUndo));
			this._suggestionImage.GetComponent<Button>().onClick.AddListener(new UnityAction(this.ApplySuggestion));
			this.ApplyPressedKey((KeyCode)PlayerPrefsSl.Get(this._setting.PlayerPrefsKey, 0));
		}

		protected override void ApplyPressedKey(KeyCode key)
		{
			base.ApplyPressedKey(key);
			this._setting.AssignedKeyCode = key;
			PlayerPrefsSl.Set(this._setting.PlayerPrefsKey, (int)key);
			this._undoImage.enabled = key > KeyCode.None;
			this._suggestionImage.enabled = key == KeyCode.None && this._setting.SuggestedKey > KeyCode.None;
		}

		private void PressUndo()
		{
			this.ApplyPressedKey(KeyCode.None);
		}

		private SSKeybindSetting _setting;

		[SerializeField]
		private Image _undoImage;

		[SerializeField]
		private Image _suggestionImage;

		[SerializeField]
		private SSEntryLabel _label;
	}
}
