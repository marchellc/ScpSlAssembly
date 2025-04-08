using System;
using System.Collections.Generic;

namespace Interactables.Interobjects.DoorUtils
{
	public static class DoorPermissionUtils
	{
		public static bool HasFlagFast(this KeycardPermissions perm, KeycardPermissions flag)
		{
			return (perm & flag) == flag;
		}

		public static KeycardPermissions TranslateObsoletePermissions(string[] obsoletePerms)
		{
			int num = 0;
			foreach (string text in obsoletePerms)
			{
				KeycardPermissions keycardPermissions;
				if (DoorPermissionUtils.BackwardsCompatibilityPermissions.TryGetValue(text, out keycardPermissions))
				{
					num = (int)((ushort)num + keycardPermissions);
				}
			}
			return (KeycardPermissions)num;
		}

		private static readonly Dictionary<string, KeycardPermissions> BackwardsCompatibilityPermissions = new Dictionary<string, KeycardPermissions>
		{
			{
				"CONT_LVL_1",
				KeycardPermissions.ContainmentLevelOne
			},
			{
				"CONT_LVL_2",
				KeycardPermissions.ContainmentLevelTwo
			},
			{
				"CONT_LVL_3",
				KeycardPermissions.ContainmentLevelThree
			},
			{
				"ARMORY_LVL_1",
				KeycardPermissions.ArmoryLevelOne
			},
			{
				"ARMORY_LVL_2",
				KeycardPermissions.ArmoryLevelTwo
			},
			{
				"ARMORY_LVL_3",
				KeycardPermissions.ArmoryLevelThree
			},
			{
				"INCOM_ACC",
				KeycardPermissions.Intercom
			},
			{
				"CHCKPOINT_ACC",
				KeycardPermissions.Checkpoints
			},
			{
				"EXIT_ACC",
				KeycardPermissions.ExitGates
			}
		};
	}
}
