using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.ControlsSettings;

public class KeybindEntry : KeycodeField
{
	private ActionName _action;

	private KeyCode _key;

	[SerializeField]
	private TMP_Text _labelText;

	[SerializeField]
	private Image _undoImage;

	private const string LabelKey = "Keybinds";

	private static readonly ActionName[] BlacklistedLMBs = new ActionName[5]
	{
		ActionName.Inventory,
		ActionName.PlayerList,
		ActionName.CharacterInfo,
		ActionName.RemoteAdmin,
		ActionName.GameConsole
	};

	public void Init(ActionName action)
	{
		this._action = action;
		this._key = NewInput.GetKey(action);
		this.RefreshKey();
		NewInput.OnKeyModified += OnKeyModified;
		this.RefreshLabel();
		TranslationReader.OnTranslationsRefreshed += RefreshLabel;
		this._undoImage.GetComponent<Button>().onClick.AddListener(delegate
		{
			NewInput.ChangeKeybind(this._action, KeyCode.None);
		});
	}

	protected override bool ValidatePressedKey(KeyCode key)
	{
		if (key == KeyCode.Mouse0)
		{
			return !KeybindEntry.BlacklistedLMBs.Contains(this._action);
		}
		return true;
	}

	protected override void ApplyPressedKey(KeyCode key)
	{
		base.ApplyPressedKey(key);
		NewInput.ChangeKeybind(this._action, key);
	}

	private void OnDestroy()
	{
		NewInput.OnKeyModified -= OnKeyModified;
		TranslationReader.OnTranslationsRefreshed -= RefreshLabel;
	}

	private void RefreshLabel()
	{
		this._labelText.text = TranslationReader.Get("Keybinds", (int)this._action, this._action.ToString());
	}

	private void RefreshKey()
	{
		base.SetDisplayedKey(this._key);
		this._undoImage.enabled = NewInput.DefaultKeybinds.TryGetValue(this._action, out var value) && value != this._key;
	}

	private void OnKeyModified(ActionName action, KeyCode key)
	{
		if (this._action == action)
		{
			this._key = key;
			this.RefreshKey();
		}
	}
}
