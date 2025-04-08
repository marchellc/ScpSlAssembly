using System;
using UnityEngine;

namespace UserSettings
{
	public class ToggleOrHoldInput
	{
		private bool ToggleMode
		{
			get
			{
				ToggleOrHoldInput.InputActivationMode mode = this._mode;
				if (mode == ToggleOrHoldInput.InputActivationMode.Toggle)
				{
					return this._trackedSetting.Value;
				}
				if (mode != ToggleOrHoldInput.InputActivationMode.Hold)
				{
					throw new InvalidOperationException(string.Format("Undefined {0} with value of {1} detected for this Toggle/Hold setting.", "InputActivationMode", this._mode));
				}
				return !this._trackedSetting.Value;
			}
		}

		private KeyCode TargetKey
		{
			get
			{
				return NewInput.GetKey(this._targetAction, KeyCode.None);
			}
		}

		public bool KeyHeld
		{
			get
			{
				return Input.GetKey(this.TargetKey);
			}
		}

		public bool KeyDown
		{
			get
			{
				return Input.GetKeyDown(this.TargetKey);
			}
		}

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
			if (!this.ToggleMode)
			{
				return;
			}
			this.ResetAll();
		}

		public ToggleOrHoldInput(ActionName targetAction, CachedUserSetting<bool> settings, ToggleOrHoldInput.InputActivationMode modeWhenTrue = ToggleOrHoldInput.InputActivationMode.Toggle)
		{
			this._targetAction = targetAction;
			this._trackedSetting = settings;
			this._mode = modeWhenTrue;
		}

		private readonly ActionName _targetAction;

		private readonly ToggleOrHoldInput.InputActivationMode _mode;

		private readonly CachedUserSetting<bool> _trackedSetting;

		private bool _toggled;

		public enum InputActivationMode
		{
			Toggle,
			Hold
		}
	}
}
