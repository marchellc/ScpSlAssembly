using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.ControlsSettings
{
	public class KeybindEntry : KeycodeField
	{
		public void Init(ActionName action)
		{
			this._action = action;
			this._key = NewInput.GetKey(action, KeyCode.None);
			this.RefreshKey();
			NewInput.OnKeyModified += this.OnKeyModified;
			this.RefreshLabel();
			TranslationReader.OnTranslationsRefreshed += this.RefreshLabel;
			this._undoImage.GetComponent<Button>().onClick.AddListener(delegate
			{
				NewInput.ChangeKeybind(this._action, KeyCode.None);
			});
		}

		protected override bool ValidatePressedKey(KeyCode key)
		{
			return key != KeyCode.Mouse0 || !KeybindEntry.BlacklistedLMBs.Contains(this._action);
		}

		protected override void ApplyPressedKey(KeyCode key)
		{
			base.ApplyPressedKey(key);
			NewInput.ChangeKeybind(this._action, key);
		}

		private void OnDestroy()
		{
			NewInput.OnKeyModified -= this.OnKeyModified;
			TranslationReader.OnTranslationsRefreshed -= this.RefreshLabel;
		}

		private void RefreshLabel()
		{
			this._labelText.text = TranslationReader.Get("Keybinds", (int)this._action, this._action.ToString());
		}

		private void RefreshKey()
		{
			base.SetDisplayedKey(this._key);
			KeyCode keyCode;
			this._undoImage.enabled = NewInput.DefaultKeybinds.TryGetValue(this._action, out keyCode) && keyCode != this._key;
		}

		private void OnKeyModified(ActionName action, KeyCode key)
		{
			if (this._action != action)
			{
				return;
			}
			this._key = key;
			this.RefreshKey();
		}

		private ActionName _action;

		private KeyCode _key;

		[SerializeField]
		private TMP_Text _labelText;

		[SerializeField]
		private Image _undoImage;

		private const string LabelKey = "Keybinds";

		private static readonly ActionName[] BlacklistedLMBs = new ActionName[]
		{
			ActionName.Inventory,
			ActionName.PlayerList,
			ActionName.CharacterInfo,
			ActionName.RemoteAdmin,
			ActionName.GameConsole
		};
	}
}
