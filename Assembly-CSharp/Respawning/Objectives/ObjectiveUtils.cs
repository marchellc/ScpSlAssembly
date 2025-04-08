using System;
using PlayerRoles;
using UnityEngine;

namespace Respawning.Objectives
{
	public static class ObjectiveUtils
	{
		public static string GetColoredNickname(this ReferenceHub hub)
		{
			string nickname = hub.GetNickname();
			return string.Concat(new string[]
			{
				"<color=",
				hub.roleManager.CurrentRole.RoleColor.ToHex(),
				">",
				nickname,
				"</color>"
			});
		}

		public static Color GetRoleColor(this RoleTypeId role)
		{
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
			{
				return Color.white;
			}
			return playerRoleBase.RoleColor;
		}

		public static string GetNickname(this ReferenceHub hub)
		{
			if (!(hub != null))
			{
				return "???";
			}
			return hub.nicknameSync.MyNick;
		}

		public static string GetFactionColor(this Faction faction)
		{
			string text;
			if (faction != Faction.FoundationStaff)
			{
				if (faction == Faction.FoundationEnemy)
				{
					text = "#008F1C";
				}
				else
				{
					text = "white";
				}
			}
			else
			{
				text = "#0096FF";
			}
			return text;
		}

		private const string NullName = "???";
	}
}
