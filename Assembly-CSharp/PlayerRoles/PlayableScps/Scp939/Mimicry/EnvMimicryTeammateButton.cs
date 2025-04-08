using System;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class EnvMimicryTeammateButton : EnvMimicryStandardButton
	{
		private bool EventAssigned
		{
			get
			{
				return this._eventAssigned;
			}
			set
			{
				if (this.EventAssigned == value)
				{
					return;
				}
				if (value)
				{
					PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
				}
				else
				{
					PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
				}
				this._eventAssigned = value;
			}
		}

		protected override bool IsAvailable
		{
			get
			{
				return this._isActive;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			if (ReferenceHub.AllHubs.Any((ReferenceHub x) => x.GetRoleId() == this._targetRole))
			{
				this._isActive = true;
				return;
			}
			this.EventAssigned = true;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			this.EventAssigned = false;
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (newRole.RoleTypeId != this._targetRole)
			{
				return;
			}
			this._isActive = true;
			this.EventAssigned = false;
		}

		[SerializeField]
		private RoleTypeId _targetRole;

		private bool _isActive;

		private bool _eventAssigned;
	}
}
