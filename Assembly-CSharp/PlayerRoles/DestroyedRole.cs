using System;
using UnityEngine;

namespace PlayerRoles
{
	public class DestroyedRole : PlayerRoleBase, IHiddenRole
	{
		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.Destroyed;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return Color.white;
			}
		}

		public override Team Team
		{
			get
			{
				return Team.Dead;
			}
		}

		public bool IsHidden
		{
			get
			{
				return true;
			}
		}
	}
}
