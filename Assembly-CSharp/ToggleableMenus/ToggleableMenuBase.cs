using CursorManagement;
using UnityEngine;

namespace ToggleableMenus;

public abstract class ToggleableMenuBase : MonoBehaviour, IRegisterableMenu, ICursorOverride
{
	private bool _isEnabled;

	public ActionName MenuActionKey;

	public abstract bool CanToggle { get; }

	public virtual CursorOverrideMode CursorOverride
	{
		get
		{
			if (!this.IsEnabled)
			{
				return CursorOverrideMode.NoOverride;
			}
			return CursorOverrideMode.Free;
		}
	}

	public virtual bool LockMovement => false;

	public virtual bool IsEnabled
	{
		get
		{
			return this._isEnabled;
		}
		set
		{
			if (value != this._isEnabled)
			{
				this._isEnabled = value;
				this.OnToggled();
			}
		}
	}

	protected abstract void OnToggled();

	protected virtual void Awake()
	{
	}

	protected virtual void OnDestroy()
	{
	}
}
