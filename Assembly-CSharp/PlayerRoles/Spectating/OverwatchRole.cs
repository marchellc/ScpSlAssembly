using TMPro;
using UnityEngine;

namespace PlayerRoles.Spectating;

public class OverwatchRole : SpectatorRole, IObfuscatedRole
{
	[SerializeField]
	private TextMeshProUGUI _hudTemplate;

	public override RoleTypeId RoleTypeId => RoleTypeId.Overwatch;

	public override Color RoleColor => Color.cyan;

	public override bool ReadyToRespawn => false;

	public RoleTypeId GetRoleForUser(ReferenceHub receiver)
	{
		base.TryGetOwner(out var hub);
		if (!(hub == receiver) && !PermissionsHandler.IsPermitted(receiver.serverRoles.Permissions, PlayerPermissions.GameplayData))
		{
			return base.RoleTypeId;
		}
		return this.RoleTypeId;
	}

	public override void DisableRole(RoleTypeId newRole)
	{
		base.DisableRole(newRole);
	}
}
