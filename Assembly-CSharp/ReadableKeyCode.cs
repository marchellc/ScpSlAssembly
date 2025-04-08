using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ReadableKeyCode
{
	public ReadableKeyCode(KeyCode keycode)
	{
		ReadableKeyCode readableKeyCode;
		if (ReadableKeyCode.AlreadyDefinedKeycodes.TryGetValue(keycode, out readableKeyCode))
		{
			this.NormalVersion = readableKeyCode.NormalVersion;
			this.ShortVersion = readableKeyCode.ShortVersion;
			this._normalVersionLength = readableKeyCode._normalVersionLength;
			return;
		}
		ReadableKeyCode.GetReadableForm(keycode, out this.NormalVersion, out this.ShortVersion);
		this._normalVersionLength = this.NormalVersion.Length;
		ReadableKeyCode.AlreadyDefinedKeycodes[keycode] = this;
	}

	public ReadableKeyCode(ActionName action)
	{
		this = new ReadableKeyCode(NewInput.GetKey(action, KeyCode.None));
	}

	public string GetBestVersion(int maxCharacters)
	{
		if (this._normalVersionLength <= maxCharacters)
		{
			return this.NormalVersion;
		}
		return this.ShortVersion;
	}

	public override string ToString()
	{
		return this.NormalVersion;
	}

	private static void GetReadableForm(KeyCode keycode, out string normalVer, out string shortVer)
	{
		if (keycode <= KeyCode.BackQuote)
		{
			if (keycode <= KeyCode.Escape)
			{
				if (keycode == KeyCode.Backspace)
				{
					normalVer = "Backspace";
					shortVer = "BKSP";
					return;
				}
				if (keycode == KeyCode.Escape)
				{
					normalVer = "Escape";
					shortVer = "Esc";
					return;
				}
			}
			else
			{
				if (keycode - KeyCode.Alpha0 <= 9)
				{
					normalVer = (keycode - KeyCode.Alpha0).ToString();
					shortVer = normalVer;
					return;
				}
				if (keycode == KeyCode.BackQuote)
				{
					normalVer = "~";
					shortVer = "~";
					return;
				}
			}
		}
		else if (keycode <= KeyCode.PageDown)
		{
			if (keycode == KeyCode.Delete)
			{
				normalVer = "Delete";
				shortVer = "Del";
				return;
			}
			switch (keycode)
			{
			case KeyCode.Keypad0:
			case KeyCode.Keypad1:
			case KeyCode.Keypad2:
			case KeyCode.Keypad3:
			case KeyCode.Keypad4:
			case KeyCode.Keypad5:
			case KeyCode.Keypad6:
			case KeyCode.Keypad7:
			case KeyCode.Keypad8:
			case KeyCode.Keypad9:
			{
				int num = keycode - KeyCode.Keypad0;
				normalVer = "Numpad " + num.ToString();
				shortVer = "Num " + num.ToString();
				return;
			}
			case KeyCode.UpArrow:
				normalVer = "Up Arrow";
				shortVer = "▲";
				return;
			case KeyCode.DownArrow:
				normalVer = "Down Arrow";
				shortVer = "▼";
				return;
			case KeyCode.RightArrow:
				normalVer = "Right Arrow";
				shortVer = "►";
				return;
			case KeyCode.LeftArrow:
				normalVer = "Left Arrow";
				shortVer = "◄";
				return;
			case KeyCode.Insert:
				normalVer = "Insert";
				shortVer = "Ins";
				return;
			case KeyCode.PageUp:
				normalVer = "Page Up";
				shortVer = "PgUp";
				return;
			case KeyCode.PageDown:
				normalVer = "Page Down";
				shortVer = "PgDn";
				return;
			}
		}
		else
		{
			switch (keycode)
			{
			case KeyCode.RightShift:
				normalVer = "Right Shift";
				shortVer = "Right ⇧";
				return;
			case KeyCode.LeftShift:
				normalVer = "Left Shift";
				shortVer = "Left ⇧";
				return;
			case KeyCode.RightControl:
				normalVer = "Right Control";
				shortVer = "R Ctrl";
				return;
			case KeyCode.LeftControl:
				normalVer = "Left Control";
				shortVer = "L Ctrl";
				return;
			case KeyCode.RightAlt:
				normalVer = "Right Alt";
				shortVer = "R Alt";
				return;
			case KeyCode.LeftAlt:
				normalVer = "Left Alt";
				shortVer = "L Alt";
				return;
			default:
				switch (keycode)
				{
				case KeyCode.Mouse0:
					normalVer = "Left Mouse Button";
					shortVer = "LMB";
					return;
				case KeyCode.Mouse1:
					normalVer = "Right Mouse Button";
					shortVer = "RMB";
					return;
				case KeyCode.Mouse2:
					normalVer = "Middle Mouse Button";
					shortVer = "MMB";
					return;
				case KeyCode.Mouse3:
				case KeyCode.Mouse4:
				case KeyCode.Mouse5:
				case KeyCode.Mouse6:
					normalVer = keycode.ToString().Insert(5, " Button ");
					shortVer = "MB" + normalVer[normalVer.Length - 1].ToString();
					return;
				}
				break;
			}
		}
		normalVer = keycode.ToString();
		shortVer = normalVer;
	}

	private static readonly Dictionary<KeyCode, ReadableKeyCode> AlreadyDefinedKeycodes = new Dictionary<KeyCode, ReadableKeyCode>();

	private readonly int _normalVersionLength;

	public readonly string NormalVersion;

	public readonly string ShortVersion;
}
