using System;
using System.Collections.Generic;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles
{
	public static class PlayerRoleLoader
	{
		public static Dictionary<RoleTypeId, PlayerRoleBase> AllRoles
		{
			get
			{
				if (!PlayerRoleLoader._loaded)
				{
					PlayerRoleLoader.LoadRoles();
				}
				return PlayerRoleLoader._loadedRoles;
			}
		}

		public static bool TryGetRoleTemplate<T>(RoleTypeId roleType, out T result)
		{
			PlayerRoleBase playerRoleBase;
			if (PlayerRoleLoader.AllRoles.TryGetValue(roleType, out playerRoleBase) && playerRoleBase is T)
			{
				T t = playerRoleBase as T;
				result = t;
				return true;
			}
			result = default(T);
			return false;
		}

		private static void LoadRoles()
		{
			PlayerRoleLoader._loadedRoles = new Dictionary<RoleTypeId, PlayerRoleBase>();
			PlayerRoleBase[] array = Resources.LoadAll<PlayerRoleBase>("Defined Roles");
			Array.Sort<PlayerRoleBase>(array, (PlayerRoleBase x, PlayerRoleBase y) => ((int)x.RoleTypeId).CompareTo((int)y.RoleTypeId));
			foreach (PlayerRoleBase playerRoleBase in array)
			{
				IHolidayRole holidayRole = playerRoleBase as IHolidayRole;
				if ((holidayRole == null || holidayRole.IsAvailable) && playerRoleBase.gameObject.activeSelf)
				{
					PlayerRoleLoader._loadedRoles[playerRoleBase.RoleTypeId] = playerRoleBase;
					PoolManager.Singleton.TryAddPool(playerRoleBase);
				}
			}
			PlayerRoleLoader._loaded = true;
			Action onLoaded = PlayerRoleLoader.OnLoaded;
			if (onLoaded == null)
			{
				return;
			}
			onLoaded();
		}

		private static bool _loaded;

		private static Dictionary<RoleTypeId, PlayerRoleBase> _loadedRoles = new Dictionary<RoleTypeId, PlayerRoleBase>();

		public static Action OnLoaded;
	}
}
