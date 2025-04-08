using System;
using System.IO;

namespace GameCore
{
	public static class ConfigSharing
	{
		static ConfigSharing()
		{
			ConfigSharing.Reload();
		}

		internal static void Reload()
		{
			ConfigSharing.Shares[0] = ConfigFile.SharingConfig.GetString("bans", "");
			ConfigSharing.Shares[1] = ConfigFile.SharingConfig.GetString("mutes", "");
			ConfigSharing.Shares[2] = ConfigFile.SharingConfig.GetString("whitelist", "");
			ConfigSharing.Shares[3] = ConfigFile.SharingConfig.GetString("reserved_slots", "");
			ConfigSharing.Shares[4] = ConfigFile.SharingConfig.GetString("groups", "");
			ConfigSharing.Shares[5] = ConfigFile.SharingConfig.GetString("groups_members", "");
			ConfigSharing.Shares[6] = ConfigFile.SharingConfig.GetString("gameplay_database", "");
			ushort num = 0;
			while ((int)num < ConfigSharing.Shares.Length)
			{
				if (ConfigSharing.Shares[(int)num] == "disable")
				{
					ConfigSharing.Paths[(int)num] = ((num == 4 || num == 5) ? null : FileManager.GetAppFolder(true, true, ""));
				}
				else
				{
					ConfigSharing.Paths[(int)num] = FileManager.GetAppFolder(true, true, ConfigSharing.Shares[(int)num]);
					string appFolder = FileManager.GetAppFolder(true, true, ConfigSharing.Shares[(int)num]);
					if (!Directory.Exists(appFolder))
					{
						Directory.CreateDirectory(appFolder);
					}
					if (num >= 4 && num <= 5)
					{
						if (num != 4)
						{
							if (num == 5)
							{
								if (!File.Exists(ConfigSharing.Paths[(int)num] + "shared_groups_members.txt"))
								{
									File.Copy("ConfigTemplates/shared_groups_members.template.txt", ConfigSharing.Paths[(int)num] + "shared_groups_members.txt");
								}
							}
						}
						else if (!File.Exists(ConfigSharing.Paths[(int)num] + "shared_groups.txt"))
						{
							File.Copy("ConfigTemplates/shared_groups.template.txt", ConfigSharing.Paths[(int)num] + "shared_groups.txt");
						}
					}
				}
				num += 1;
			}
			ServerConsole.AddLog("Config sharing loaded.", ConsoleColor.Gray, false);
		}

		public static readonly string[] Shares = new string[7];

		public static readonly string[] Paths = new string[7];

		public enum ConfigShare
		{
			Bans,
			Mutes,
			Whitelist,
			ReservedSlots,
			Groups,
			GroupsMembers,
			GameplayDatabase
		}
	}
}
