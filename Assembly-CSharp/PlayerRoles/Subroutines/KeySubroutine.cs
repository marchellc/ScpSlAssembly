using System;
using UnityEngine;

namespace PlayerRoles.Subroutines
{
	public abstract class KeySubroutine<T> : StandardSubroutine<T> where T : PlayerRoleBase
	{
		protected abstract ActionName TargetKey { get; }

		protected virtual bool IsKeyHeld
		{
			get
			{
				return this._held;
			}
			set
			{
				if (value == this._held)
				{
					return;
				}
				this._held = value;
				if (value)
				{
					this.OnKeyDown();
					return;
				}
				this.OnKeyUp();
			}
		}

		protected virtual bool KeyPressable
		{
			get
			{
				return base.Owner.isLocalPlayer && !Cursor.visible;
			}
		}

		protected virtual bool KeyReleasable
		{
			get
			{
				return true;
			}
		}

		protected virtual void Update()
		{
			if (this.KeyPressable && Input.GetKey(NewInput.GetKey(this.TargetKey, KeyCode.None)))
			{
				this.IsKeyHeld = true;
				return;
			}
			if (this.IsKeyHeld && this.KeyReleasable)
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

		private bool _held;
	}
}
