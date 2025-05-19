using InventorySystem.Items;

namespace Interactables.Interobjects.DoorUtils;

public static class DoorPermissionsPolicyExtensions
{
	public static bool HasFlagAll(this DoorPermissionFlags perm, DoorPermissionFlags flag)
	{
		return (perm & flag) == flag;
	}

	public static bool HasFlagAny(this DoorPermissionFlags perm, DoorPermissionFlags flag)
	{
		return (perm & flag) != 0;
	}

	public static bool CheckPermissions(this IDoorPermissionRequester requester, IDoorPermissionProvider provider, out PermissionUsed callback)
	{
		return requester.PermissionsPolicy.CheckPermissions(provider, requester, out callback);
	}

	public static bool CheckPermissions(this IDoorPermissionRequester requester, ReferenceHub player, out PermissionUsed callback)
	{
		return requester.PermissionsPolicy.CheckPermissions(player, requester, out callback);
	}

	public static DoorPermissionFlags GetCombinedPermissions(this ReferenceHub hub, IDoorPermissionRequester requester)
	{
		if (hub == null)
		{
			return DoorPermissionFlags.None;
		}
		if (hub.serverRoles.BypassMode)
		{
			return DoorPermissionFlags.All;
		}
		DoorPermissionFlags doorPermissionFlags = DoorPermissionFlags.None;
		if (hub.roleManager.CurrentRole is IDoorPermissionProvider doorPermissionProvider)
		{
			doorPermissionFlags |= doorPermissionProvider.GetPermissions(requester);
		}
		ItemBase curInstance = hub.inventory.CurInstance;
		if (curInstance != null && curInstance is IDoorPermissionProvider doorPermissionProvider2)
		{
			doorPermissionFlags |= doorPermissionProvider2.GetPermissions(requester);
		}
		return doorPermissionFlags;
	}
}
