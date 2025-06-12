using System;
using UnityEngine;

namespace UserSettings;

public class ToggleOrHoldInput
{
	public enum InputActivationMode
	{
		Toggle,
		Hold
	}

	private readonly ActionName _targetAction;

	private readonly InputActivationMode _mode;

	private readonly CachedUserSetting<bool> _trackedSetting;

	private bool _toggled;

	private bool ToggleMode => this._mode switch
	{
		InputActivationMode.Toggle => this._trackedSetting.Value, 
		InputActivationMode.Hold => !this._trackedSetting.Value, 
		_ => throw new InvalidOperationException(string.Format("Undefined {0} with value of {1} detected for this Toggle/Hold setting.", "InputActivationMode", this._mode)), 
	};

	private KeyCode TargetKey => NewInput.GetKey(this._targetAction);

	public bool KeyHeld => Input.GetKey(this.TargetKey);

	public bool KeyDown => Input.GetKeyDown(this.TargetKey);

	public bool IsActive
	{
		get
		{
			if (this.ToggleMode)
			{
				if (this.KeyDown)
				{
					this._toggled = !this._toggled;
				}
			}
			else
			{
				if (this.KeyDown)
				{
					this._toggled = true;
				}
				if (!this.KeyHeld)
				{
					this._toggled = false;
				}
			}
			return this._toggled;
		}
	}

	public void ResetAll()
	{
		this._toggled = false;
	}

	public void ResetToggle()
	{
		if (this.ToggleMode)
		{
			this.ResetAll();
		}
	}

	public ToggleOrHoldInput(ActionName targetAction, CachedUserSetting<bool> settings, InputActivationMode modeWhenTrue = InputActivationMode.Toggle)
	{
		this._targetAction = targetAction;
		this._trackedSetting = settings;
		this._mode = modeWhenTrue;
	}
}
