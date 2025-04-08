using System;
using System.Collections.Generic;
using System.IO;
using CentralAuth;
using GameCore;

public static class ReservedSlot
{
	static ReservedSlot()
	{
		ReservedSlot.Reload();
	}

	public static void Reload()
	{
		string text = ConfigSharing.Paths[3] + "UserIDReservedSlots.txt";
		ReservedSlot.Users.Clear();
		if (!File.Exists(text))
		{
			FileManager.WriteStringToFile("#Put one UserID (eg. 76561198071934271@steam or 274613382353518592@discord) per line. Lines prefixed with \"#\" are ignored.", text);
			return;
		}
		using (StreamReader streamReader = new StreamReader(text))
		{
			string text2;
			while ((text2 = streamReader.ReadLine()) != null)
			{
				if (!string.IsNullOrWhiteSpace(text2) && !text2.TrimStart().StartsWith("#", StringComparison.Ordinal) && text2.Contains("@"))
				{
					ReservedSlot.Users.Add(text2.Trim());
				}
			}
		}
		ServerConsole.AddLog("Reserved slots list has been loaded.", ConsoleColor.Gray, false);
	}

	public static bool HasReservedSlot(string userId)
	{
		return ReservedSlot.Users.Contains(userId.Trim()) || !PlayerAuthenticationManager.OnlineMode;
	}

	public static readonly HashSet<string> Users = new HashSet<string>();
}
