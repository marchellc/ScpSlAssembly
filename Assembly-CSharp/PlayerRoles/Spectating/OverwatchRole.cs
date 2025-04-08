using System;
using TMPro;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public class OverwatchRole : SpectatorRole, IObfuscatedRole
	{
		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.Overwatch;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return Color.cyan;
			}
		}

		public override bool ReadyToRespawn
		{
			get
			{
				return false;
			}
		}

		public RoleTypeId GetRoleForUser(ReferenceHub receiver)
		{
			ReferenceHub referenceHub;
			base.TryGetOwner(out referenceHub);
			if (!(referenceHub == receiver) && !PermissionsHandler.IsPermitted(receiver.serverRoles.Permissions, PlayerPermissions.GameplayData))
			{
				return base.RoleTypeId;
			}
			return this.RoleTypeId;
		}

		public override void DisableRole(RoleTypeId newRole)
		{
			base.DisableRole(newRole);
		}

		[SerializeField]
		private TextMeshProUGUI _hudTemplate;
	}
}
