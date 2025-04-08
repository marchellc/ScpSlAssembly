using System;
using System.Collections.Generic;
using System.IO;
using GameCore;

public static class WhiteList
{
	static WhiteList()
	{
		WhiteList.Reload();
	}

	public static void Reload()
	{
		WhiteList.WhitelistEnabled = ConfigFile.ServerConfig.GetBool("enable_whitelist", false);
		string text = ConfigSharing.Paths[2] + "UserIDWhitelist.txt";
		WhiteList.Users.Clear();
		if (!File.Exists(text))
		{
			FileManager.WriteStringToFile("#Put one UserID (eg. 76561198071934271@steam or 274613382353518592@discord) per line. Lines prefixed with \"#\" are ignored.", text);
			return;
		}
		using (StreamReader streamReader = new StreamReader(text))
		{
			for (;;)
			{
				string text2 = streamReader.ReadLine();
				if (text2 == null)
				{
					break;
				}
				if (!string.IsNullOrWhiteSpace(text2) && !text2.TrimStart().StartsWith("#", StringComparison.Ordinal) && text2.Contains("@", StringComparison.Ordinal))
				{
					WhiteList.Users.Add(text2.Trim());
				}
			}
		}
		ServerConsole.AddLog("Whitelist has been loaded.", ConsoleColor.Gray, false);
	}

	public static bool IsOnWhitelist(string userId)
	{
		return WhiteList.Users.Contains(userId);
	}

	public static bool IsWhitelisted(string userId)
	{
		return !WhiteList.WhitelistEnabled || WhiteList.Users.Contains(userId);
	}

	public static bool WhitelistEnabled;

	public static readonly HashSet<string> Users = new HashSet<string>();
}
