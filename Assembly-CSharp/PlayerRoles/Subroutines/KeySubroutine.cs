using UnityEngine;

namespace PlayerRoles.Subroutines;

public abstract class KeySubroutine<T> : StandardSubroutine<T> where T : PlayerRoleBase
{
	private bool _held;

	protected abstract ActionName TargetKey { get; }

	protected virtual bool IsKeyHeld
	{
		get
		{
			return this._held;
		}
		set
		{
			if (value != this._held)
			{
				this._held = value;
				if (value)
				{
					this.OnKeyDown();
				}
				else
				{
					this.OnKeyUp();
				}
			}
		}
	}

	protected virtual bool KeyPressable
	{
		get
		{
			if (!base.Role.IsEmulatedDummy)
			{
				if (base.Role.IsLocalPlayer)
				{
					return !Cursor.visible;
				}
				return false;
			}
			return true;
		}
	}

	protected virtual bool KeyReleasable => true;

	protected virtual void Update()
	{
		if (this.KeyPressable && base.GetAction(this.TargetKey))
		{
			this.IsKeyHeld = true;
		}
		else if (this.IsKeyHeld && this.KeyReleasable)
		{
			this.IsKeyHeld = false;
		}
	}

	protected virtual void OnKeyDown()
	{
	}

	protected virtual void OnKeyUp()
	{
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._held = false;
	}
}
