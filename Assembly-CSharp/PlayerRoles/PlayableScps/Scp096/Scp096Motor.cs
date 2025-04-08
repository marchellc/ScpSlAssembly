using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096Motor : FpcMotor
	{
		protected override Vector3 DesiredMove
		{
			get
			{
				if (!this._role.IsLocalPlayer || !this._hasOverride)
				{
					return base.DesiredMove;
				}
				this._hasOverride = false;
				return this._overrideDir;
			}
		}

		public void SetOverride(Vector3 desiredMove)
		{
			this._hasOverride = true;
			this._overrideDir = desiredMove;
		}

		public Scp096Motor(ReferenceHub hub, Scp096Role role)
			: base(hub, role.FpcModule, false)
		{
			this._role = role;
		}

		private readonly Scp096Role _role;

		private bool _hasOverride;

		private Vector3 _overrideDir;
	}
}
