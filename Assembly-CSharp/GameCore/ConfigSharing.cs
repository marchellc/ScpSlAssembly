using System.IO;

namespace GameCore;

public static class ConfigSharing
{
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

	public static readonly string[] Shares;

	public static readonly string[] Paths;

	static ConfigSharing()
	{
		ConfigSharing.Shares = new string[7];
		ConfigSharing.Paths = new string[7];
		ConfigSharing.Reload();
	}

	internal static void Reload()
	{
		ConfigSharing.Shares[0] = ConfigFile.SharingConfig.GetString("bans");
		ConfigSharing.Shares[1] = ConfigFile.SharingConfig.GetString("mutes");
		ConfigSharing.Shares[2] = ConfigFile.SharingConfig.GetString("whitelist");
		ConfigSharing.Shares[3] = ConfigFile.SharingConfig.GetString("reserved_slots");
		ConfigSharing.Shares[4] = ConfigFile.SharingConfig.GetString("groups");
		ConfigSharing.Shares[5] = ConfigFile.SharingConfig.GetString("groups_members");
		ConfigSharing.Shares[6] = ConfigFile.SharingConfig.GetString("gameplay_database");
		for (ushort num = 0; num < ConfigSharing.Shares.Length; num++)
		{
			if (ConfigSharing.Shares[num] == "disable")
			{
				ConfigSharing.Paths[num] = ((num == 4 || num == 5) ? null : FileManager.GetAppFolder(addSeparator: true, serverConfig: true));
			}
			else
			{
				ConfigSharing.Paths[num] = FileManager.GetAppFolder(addSeparator: true, serverConfig: true, ConfigSharing.Shares[num]);
				string appFolder = FileManager.GetAppFolder(addSeparator: true, serverConfig: true, ConfigSharing.Shares[num]);
				if (!Directory.Exists(appFolder))
				{
					Directory.CreateDirectory(appFolder);
				}
				switch (num)
				{
				case 5:
					if (num == 5 && !File.Exists(ConfigSharing.Paths[num] + "shared_groups_members.txt"))
					{
						File.Copy("ConfigTemplates/shared_groups_members.template.txt", ConfigSharing.Paths[num] + "shared_groups_members.txt");
					}
					break;
				case 4:
					if (!File.Exists(ConfigSharing.Paths[num] + "shared_groups.txt"))
					{
						File.Copy("ConfigTemplates/shared_groups.template.txt", ConfigSharing.Paths[num] + "shared_groups.txt");
					}
					break;
				}
			}
		}
		ServerConsole.AddLog("Config sharing loaded.");
	}
}
