using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeycodeField : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	[SerializeField]
	private TMP_Text _keyText;

	[SerializeField]
	private KeyCode[] _cancelKeys = new KeyCode[1] { KeyCode.Escape };

	[SerializeField]
	private TextAlignmentOptions _readAlignment = TextAlignmentOptions.Center;

	[SerializeField]
	private TextAlignmentOptions _setAlignment = TextAlignmentOptions.Center;

	private const int InputCooldownFrames = 3;

	private bool _currentlyEditing;

	private int _inputCooldown;

	private KeyCode _requestedChange;

	private static KeycodeField _lastEntry;

	private static KeyCode[] _allKeyCodes;

	public KeyCode CurDisplayedKey { get; private set; }

	public virtual string NoneSymbol => "";

	public virtual string ReadSymbol => "•  •  •";

	public event Action<KeyCode> OnKeySet;

	public void SetDisplayedKey(KeyCode key)
	{
		this.CurDisplayedKey = key;
		this._keyText.text = ((key == KeyCode.None) ? this.NoneSymbol : new ReadableKeyCode(key).NormalVersion);
		this._keyText.alignment = this._setAlignment;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			KeycodeField._lastEntry = this;
			this._keyText.text = this.ReadSymbol;
			this._keyText.alignment = this._readAlignment;
			if (!this._currentlyEditing)
			{
				this._currentlyEditing = true;
				this._requestedChange = KeyCode.None;
				this._inputCooldown = 3;
			}
		}
	}

	public void ExitEditMode()
	{
		this._requestedChange = KeyCode.None;
		if (this._currentlyEditing)
		{
			this.SetDisplayedKey(this.CurDisplayedKey);
			this._currentlyEditing = false;
		}
	}

	protected virtual bool ValidatePressedKey(KeyCode key)
	{
		return true;
	}

	protected virtual void ApplyPressedKey(KeyCode key)
	{
		this.SetDisplayedKey(key);
		this.OnKeySet?.Invoke(key);
		this._currentlyEditing = false;
	}

	protected virtual void OnDisable()
	{
		this.ExitEditMode();
	}

	protected virtual void Update()
	{
		if (!this._currentlyEditing)
		{
			return;
		}
		if (KeycodeField._lastEntry != this || this._cancelKeys.Contains(this._requestedChange))
		{
			this.ExitEditMode();
			return;
		}
		if (this._requestedChange != KeyCode.None)
		{
			if (this.ValidatePressedKey(this._requestedChange))
			{
				this.ApplyPressedKey(this._requestedChange);
				return;
			}
			this._requestedChange = KeyCode.None;
		}
		if (this._inputCooldown > 0)
		{
			this._inputCooldown--;
			return;
		}
		KeyCode[] values = EnumUtils<KeyCode>.Values;
		foreach (KeyCode keyCode in values)
		{
			if (Input.GetKeyUp(keyCode))
			{
				this._requestedChange = keyCode;
			}
		}
	}
}
