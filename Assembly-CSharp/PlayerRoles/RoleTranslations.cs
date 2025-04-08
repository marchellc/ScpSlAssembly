using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles
{
	public static class RoleTranslations
	{
		public static string GetRoleName(RoleTypeId rt)
		{
			string text;
			if (!RoleTranslations.TranslatedNames.TryGetValue(rt, out text))
			{
				return rt.ToString();
			}
			return text;
		}

		public static string GetAbbreviatedRoleName(this RoleTypeId rt)
		{
			string text;
			if (!RoleTranslations.TranslatedAbbreviatedNames.TryGetValue(rt, out text))
			{
				return RoleTranslations.GetRoleName(rt);
			}
			return text;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			TranslationReader.OnTranslationsRefreshed += RoleTranslations.ReloadNames;
		}

		private static void ReloadNames()
		{
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
			{
				int key = (int)keyValuePair.Key;
				string text;
				if (key >= 0 && TranslationReader.TryGet("Class_Names", key, out text))
				{
					RoleTranslations.TranslatedNames[keyValuePair.Key] = text;
				}
				if (TranslationReader.TryGet("RA_RoleManagement", key + 1, out text))
				{
					RoleTranslations.TranslatedAbbreviatedNames[keyValuePair.Key] = text;
				}
			}
		}

		private static readonly Dictionary<RoleTypeId, string> TranslatedNames = new Dictionary<RoleTypeId, string>();

		private static readonly Dictionary<RoleTypeId, string> TranslatedAbbreviatedNames = new Dictionary<RoleTypeId, string>();

		public const string RoleNamesFile = "Class_Names";

		public const string AbbreviatedRoleNamesFile = "RA_RoleManagement";
	}
}
