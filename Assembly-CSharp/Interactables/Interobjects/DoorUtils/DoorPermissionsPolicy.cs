using System;
using InventorySystem.Items;

namespace Interactables.Interobjects.DoorUtils;

[Serializable]
public struct DoorPermissionsPolicy
{
	public DoorPermissionFlags RequiredPermissions;

	public bool RequireAll;

	public bool Bypass2176;

	public DoorPermissionsPolicy(DoorPermissionFlags requiredPermissions, bool requireAll = false, bool bypass2176 = false)
	{
		RequiredPermissions = requiredPermissions;
		RequireAll = requireAll;
		Bypass2176 = bypass2176;
	}

	public readonly bool CheckPermissions(IDoorPermissionProvider provider, IDoorPermissionRequester requester, out PermissionUsed callback)
	{
		if (RequiredPermissions == DoorPermissionFlags.None)
		{
			callback = null;
			return true;
		}
		callback = provider.PermissionsUsedCallback;
		DoorPermissionFlags permissions = provider.GetPermissions(requester);
		return CheckPermissions(permissions);
	}

	public readonly bool CheckPermissions(DoorPermissionFlags flags)
	{
		if (!(RequireAll ? flags.HasFlagAll(RequiredPermissions) : flags.HasFlagAny(RequiredPermissions)))
		{
			return RequiredPermissions == DoorPermissionFlags.None;
		}
		return true;
	}

	public readonly bool CheckPermissions(ReferenceHub hub, IDoorPermissionRequester requester, out PermissionUsed callback)
	{
		callback = null;
		if (RequiredPermissions == DoorPermissionFlags.None)
		{
			return true;
		}
		if (hub.serverRoles.BypassMode)
		{
			return true;
		}
		if (hub.roleManager.CurrentRole is IDoorPermissionProvider provider)
		{
			return CheckPermissions(provider, requester, out callback);
		}
		ItemBase curInstance = hub.inventory.CurInstance;
		if (curInstance != null && curInstance is IDoorPermissionProvider provider2)
		{
			return CheckPermissions(provider2, requester, out callback);
		}
		return false;
	}
}
