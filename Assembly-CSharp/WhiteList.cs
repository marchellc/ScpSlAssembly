using System;
using System.Collections.Generic;
using System.IO;
using GameCore;

public static class WhiteList
{
	public static bool WhitelistEnabled;

	public static readonly HashSet<string> Users;

	static WhiteList()
	{
		WhiteList.Users = new HashSet<string>();
		WhiteList.Reload();
	}

	public static void Reload()
	{
		WhiteList.WhitelistEnabled = ConfigFile.ServerConfig.GetBool("enable_whitelist");
		string path = ConfigSharing.Paths[2] + "UserIDWhitelist.txt";
		WhiteList.Users.Clear();
		if (!File.Exists(path))
		{
			FileManager.WriteStringToFile("#Put one UserID (eg. 76561198071934271@steam or 274613382353518592@discord) per line. Lines prefixed with \"#\" are ignored.", path);
			return;
		}
		using (StreamReader streamReader = new StreamReader(path))
		{
			while (true)
			{
				string text = streamReader.ReadLine();
				if (text != null)
				{
					if (!string.IsNullOrWhiteSpace(text) && !text.TrimStart().StartsWith("#", StringComparison.Ordinal) && text.Contains("@", StringComparison.Ordinal))
					{
						WhiteList.Users.Add(text.Trim());
					}
					continue;
				}
				break;
			}
		}
		ServerConsole.AddLog("Whitelist has been loaded.");
	}

	public static bool IsOnWhitelist(string userId)
	{
		return WhiteList.Users.Contains(userId);
	}

	public static bool IsWhitelisted(string userId)
	{
		if (WhiteList.WhitelistEnabled)
		{
			return WhiteList.Users.Contains(userId);
		}
		return true;
	}
}
