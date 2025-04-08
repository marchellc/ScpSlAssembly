using System;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using PlayerRoles;

namespace Interactables.Interobjects.DoorUtils
{
	[Serializable]
	public class DoorPermissions
	{
		public bool CheckPermissions(ItemBase item, ReferenceHub ply)
		{
			if (this.RequiredPermissions == KeycardPermissions.None)
			{
				return true;
			}
			if (ply != null)
			{
				if (ply.serverRoles.BypassMode)
				{
					return true;
				}
				if (item == null)
				{
					return ply.IsSCP(true) && this.RequiredPermissions.HasFlagFast(KeycardPermissions.ScpOverride);
				}
			}
			KeycardItem keycardItem = item as KeycardItem;
			if (keycardItem == null)
			{
				return false;
			}
			if (!this.RequireAll)
			{
				return (keycardItem.Permissions & this.RequiredPermissions) > KeycardPermissions.None;
			}
			return (keycardItem.Permissions & this.RequiredPermissions) == this.RequiredPermissions;
		}

		public KeycardPermissions RequiredPermissions;

		public bool RequireAll;

		public bool Bypass2176;
	}
}
