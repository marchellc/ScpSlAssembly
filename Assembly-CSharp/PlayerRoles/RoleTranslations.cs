using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles;

public static class RoleTranslations
{
	private static readonly Dictionary<RoleTypeId, string> TranslatedNames = new Dictionary<RoleTypeId, string>();

	private static readonly Dictionary<RoleTypeId, string> TranslatedAbbreviatedNames = new Dictionary<RoleTypeId, string>();

	public const string RoleNamesFile = "Class_Names";

	public const string AbbreviatedRoleNamesFile = "RA_RoleManagement";

	public static string GetRoleName(RoleTypeId rt)
	{
		if (!RoleTranslations.TranslatedNames.TryGetValue(rt, out var value))
		{
			return rt.ToString();
		}
		return value;
	}

	public static string GetAbbreviatedRoleName(this RoleTypeId rt)
	{
		if (!RoleTranslations.TranslatedAbbreviatedNames.TryGetValue(rt, out var value))
		{
			return RoleTranslations.GetRoleName(rt);
		}
		return value;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		TranslationReader.OnTranslationsRefreshed += ReloadNames;
	}

	private static void ReloadNames()
	{
		foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> allRole in PlayerRoleLoader.AllRoles)
		{
			int key = (int)allRole.Key;
			if (key >= 0 && TranslationReader.TryGet("Class_Names", key, out var val))
			{
				RoleTranslations.TranslatedNames[allRole.Key] = val;
			}
			if (TranslationReader.TryGet("RA_RoleManagement", key + 1, out val))
			{
				RoleTranslations.TranslatedAbbreviatedNames[allRole.Key] = val;
			}
		}
	}
}
