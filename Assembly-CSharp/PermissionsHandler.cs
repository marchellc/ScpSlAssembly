using System;
using System.Collections.Generic;
using System.Linq;
using GameCore;

public class PermissionsHandler
{
	public PermissionsHandler(ref YamlConfig configuration, ref YamlConfig sharedGroups, ref YamlConfig sharedGroupsMembers)
	{
		this._config = new YamlConfig(configuration.Path);
		this._sharedGroups = sharedGroups;
		this.OverridePassword = configuration.GetString("override_password", "none");
		this._overrideRole = configuration.GetString("override_password_role", "owner");
		this.StaffAccess = configuration.GetBool("enable_staff_access", false);
		this._managerAccess = configuration.GetBool("enable_manager_access", true);
		this._banTeamAccess = configuration.GetBool("enable_banteam_access", true);
		this._banTeamSlots = configuration.GetBool("enable_banteam_reserved_slots", true);
		this._banTeamGeoBypass = configuration.GetBool("enable_banteam_bypass_geoblocking", true);
		this.NorthwoodAccess = configuration.GetBool("enable_northwood_access", false);
		if (this.NorthwoodAccess)
		{
			ServerConsole.AddLog("WARNING - Northwood staff access is enabled! All NW Studios staff members will have FULL Remote Admin access (this should only be used on testing servers)! You can disable this by setting 'enable_northwood_access' to false in your remote admin config file.", ConsoleColor.Yellow, false);
		}
		this.Groups = new Dictionary<string, UserGroup>();
		this._raPermissions = new HashSet<ulong>();
		List<string> stringList = configuration.GetStringList("Roles");
		List<string> stringList2 = configuration.GetStringList("Roles");
		if (sharedGroups != null)
		{
			List<string> stringList3 = sharedGroups.GetStringList("SharedRoles");
			string text = ConfigFile.SharingConfig.GetString("groups_sharing_mode", "").ToLowerInvariant();
			if (!(text == "all"))
			{
				if (!(text == "opt-in"))
				{
					if (!(text == "opt-out"))
					{
						ServerConsole.AddLog("Invalid group sharing mode set!", ConsoleColor.Gray, false);
					}
					else
					{
						List<string> optOut = ConfigFile.SharingConfig.GetStringList("groups_opt_out_list");
						stringList.AddRange(stringList3.Where((string group) => !optOut.Contains(group)));
					}
				}
				else
				{
					List<string> optIn = ConfigFile.SharingConfig.GetStringList("groups_opt_in_list");
					stringList.AddRange(stringList3.Where((string group) => optIn.Contains(group)));
				}
			}
			else
			{
				stringList.AddRange(stringList3);
			}
		}
		string[] array = configuration.GetKeys().ToArray<string>();
		foreach (string text2 in stringList)
		{
			string text3 = ((array.Contains(text2 + "_badge") || sharedGroups == null) ? configuration.GetString(text2 + "_badge", "") : sharedGroups.GetString(text2 + "_badge", ""));
			string text4 = ((array.Contains(text2 + "_color") || sharedGroups == null) ? configuration.GetString(text2 + "_color", "") : sharedGroups.GetString(text2 + "_color", ""));
			bool flag = ((array.Contains(text2 + "_cover") || sharedGroups == null) ? configuration.GetBool(text2 + "_cover", true) : sharedGroups.GetBool(text2 + "_cover", true));
			bool flag2 = ((array.Contains(text2 + "_hidden") || sharedGroups == null) ? configuration.GetBool(text2 + "_hidden", false) : sharedGroups.GetBool(text2 + "_hidden", false));
			byte b = ((array.Contains(text2 + "_kick_power") || sharedGroups == null) ? configuration.GetByte(text2 + "_kick_power", 0) : sharedGroups.GetByte(text2 + "_kick_power", 0));
			byte b2 = ((array.Contains(text2 + "_required_kick_power") || sharedGroups == null) ? configuration.GetByte(text2 + "_required_kick_power", 0) : sharedGroups.GetByte(text2 + "_required_kick_power", 0));
			if (!(text3 == "") && !(text4 == ""))
			{
				if (this.Groups.ContainsKey(text2))
				{
					ServerConsole.AddLog("Duplicated group definition: " + text2 + ".", ConsoleColor.Gray, false);
				}
				else
				{
					this.Groups.Add(text2, new UserGroup
					{
						Name = text2,
						BadgeColor = text4,
						BadgeText = text3,
						Permissions = 0UL,
						Cover = flag,
						HiddenByDefault = flag2,
						Shared = !stringList2.Contains(text2),
						KickPower = b,
						RequiredKickPower = b2
					});
				}
			}
		}
		this.Members = configuration.GetStringDictionary("Members");
		YamlConfig yamlConfig = sharedGroupsMembers;
		Dictionary<string, string> dictionary = ((yamlConfig != null) ? yamlConfig.GetStringDictionary("SharedMembers") : null);
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				string text5;
				if (this.Members.TryGetValue(keyValuePair.Key, out text5))
				{
					ServerConsole.AddLog(string.Concat(new string[] { "Duplicated group member: ", keyValuePair.Key, ". Is member of ", text5, " and ", keyValuePair.Value, "." }), ConsoleColor.Gray, false);
				}
				else
				{
					this.Members.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}
		this._lastPerm = 1UL;
		HashSet<string> hashSet = new HashSet<string>();
		if (this.Members != null)
		{
			foreach (KeyValuePair<string, string> keyValuePair2 in this.Members)
			{
				if (!this.Groups.ContainsKey(keyValuePair2.Value))
				{
					hashSet.Add(keyValuePair2.Key);
				}
			}
		}
		if (hashSet.Count > 0 && this.Members != null)
		{
			foreach (string text6 in hashSet)
			{
				this.Members.Remove(text6);
			}
		}
		hashSet.Clear();
		this.Permissions = new Dictionary<string, ulong>();
		foreach (string text7 in EnumUtils<PlayerPermissions>.Names)
		{
			ulong num = (ulong)Enum.Parse(typeof(PlayerPermissions), text7);
			this.FullPerm |= num;
			this.Permissions.Add(text7, num);
			if (num != 4096UL && num != 131072UL && num != 2097152UL && num != 4194304UL && num != 16777216UL && num != 134217728UL && num != 8388608UL)
			{
				this._raPermissions.Add(num);
			}
			if (num > this._lastPerm)
			{
				this._lastPerm = num;
			}
		}
		this.RefreshPermissions();
	}

	public ulong RegisterPermission(string name, bool remoteAdmin, bool refresh = true)
	{
		this._lastPerm = (ulong)Math.Pow(2.0, Math.Log(this._lastPerm, 2.0) + 1.0);
		this.FullPerm |= this._lastPerm;
		this.Permissions.Add(name, this._lastPerm);
		if (remoteAdmin)
		{
			this._raPermissions.Add(this._lastPerm);
		}
		if (refresh)
		{
			this.RefreshPermissions();
		}
		return this._lastPerm;
	}

	public void RefreshPermissions()
	{
		foreach (KeyValuePair<string, UserGroup> keyValuePair in this.Groups)
		{
			keyValuePair.Value.Permissions = 0UL;
		}
		Dictionary<string, string> stringDictionary = this._config.GetStringDictionary("Permissions");
		YamlConfig sharedGroups = this._sharedGroups;
		Dictionary<string, string> dictionary = ((sharedGroups != null) ? sharedGroups.GetStringDictionary("SharedPermissions") : null);
		foreach (string text in this.Permissions.Keys)
		{
			ulong num = this.Permissions[text];
			string text2;
			if (stringDictionary.TryGetValue(text, out text2))
			{
				string[] array = YamlConfig.ParseCommaSeparatedString(text2);
				if (array == null)
				{
					ServerConsole.AddLog("Failed to process group permissions in remote admin config! Make sure there is no typo.", ConsoleColor.Gray, false);
				}
				else
				{
					foreach (string text3 in array)
					{
						UserGroup userGroup;
						if (this.Groups.TryGetValue(text3, out userGroup))
						{
							userGroup.Permissions |= num;
						}
					}
				}
			}
			else
			{
				ServerConsole.AddLog("RemoteAdmin config is missing permission definition: " + text, ConsoleColor.Gray, false);
			}
			if (dictionary != null)
			{
				string text4;
				if (dictionary.TryGetValue(text, out text4))
				{
					string[] array3 = YamlConfig.ParseCommaSeparatedString(text4);
					if (array3 != null)
					{
						foreach (string text5 in array3)
						{
							UserGroup userGroup2;
							if (this.Groups.TryGetValue(text5, out userGroup2))
							{
								userGroup2.Permissions |= num;
							}
						}
					}
				}
				else
				{
					ServerConsole.AddLog("Shared groups config is missing permission definition: " + text, ConsoleColor.Gray, false);
				}
			}
		}
	}

	public bool IsRaPermitted(ulong permissions)
	{
		return this._raPermissions.Any((ulong perm) => PermissionsHandler.IsPermitted(permissions, perm));
	}

	public UserGroup GetGroup(string name)
	{
		if (this.Groups.ContainsKey(name))
		{
			return this.Groups[name].Clone();
		}
		return null;
	}

	public List<string> GetAllGroupsNames()
	{
		return this.Groups.Keys.ToList<string>();
	}

	public Dictionary<string, UserGroup> GetAllGroups()
	{
		return this.Groups.Keys.ToDictionary((string gr) => gr, (string gr) => this.Groups[gr]);
	}

	public string GetPermissionName(ulong value)
	{
		return this.Permissions.FirstOrDefault((KeyValuePair<string, ulong> x) => x.Value == value).Key;
	}

	public ulong GetPermissionValue(string name)
	{
		return this.Permissions.FirstOrDefault((KeyValuePair<string, ulong> x) => x.Key == name).Value;
	}

	public List<string> GetAllPermissions()
	{
		return this.Permissions.Keys.ToList<string>();
	}

	public bool BanTeamSlots
	{
		get
		{
			return this._banTeamSlots || CustomNetworkManager.IsVerified;
		}
	}

	public bool BanTeamBypassGeo
	{
		get
		{
			return this._banTeamGeoBypass || CustomNetworkManager.IsVerified;
		}
	}

	public static bool IsPermitted(ulong permissions, PlayerPermissions check)
	{
		return PermissionsHandler.IsPermitted(permissions, (ulong)check);
	}

	public static bool IsPermitted(ulong permissions, PlayerPermissions[] check)
	{
		if (check.Length == 0)
		{
			return true;
		}
		ulong num = check.Aggregate(0UL, (ulong current, PlayerPermissions c) => current | (ulong)c);
		return PermissionsHandler.IsPermitted(permissions, num);
	}

	public bool IsPermitted(ulong permissions, string check)
	{
		return this.Permissions.ContainsKey(check) && PermissionsHandler.IsPermitted(permissions, this.Permissions[check]);
	}

	public bool IsPermitted(ulong permissions, string[] check)
	{
		if (check.Length == 0)
		{
			return true;
		}
		ulong num = check.Where((string c) => this.Permissions.ContainsKey(c)).Aggregate(0UL, (ulong current, string c) => current | this.Permissions[c]);
		return PermissionsHandler.IsPermitted(permissions, num);
	}

	public static bool IsPermitted(ulong permissions, ulong check)
	{
		return (permissions & check) > 0UL;
	}

	public UserGroup OverrideGroup
	{
		get
		{
			if (!this.OverrideEnabled)
			{
				return null;
			}
			if (this.Groups.ContainsKey(this._overrideRole))
			{
				return this.Groups[this._overrideRole];
			}
			return null;
		}
	}

	public bool OverrideEnabled
	{
		get
		{
			if (string.IsNullOrEmpty(this.OverridePassword) || this.OverridePassword == "none")
			{
				return false;
			}
			if (!CustomNetworkManager.IsVerified)
			{
				return true;
			}
			if (this.OverridePassword.Length < 8)
			{
				ServerConsole.AddLog("Override password refused, because it's too short (requirement for verified servers only).", ConsoleColor.Gray, false);
				return false;
			}
			if (this.OverridePassword.ToLower() == this.OverridePassword || this.OverridePassword.ToUpper() == this.OverridePassword)
			{
				ServerConsole.AddLog("Override password refused, because it must contain mixed case chars (requirement for verified servers only).", ConsoleColor.Gray, false);
				return false;
			}
			if (this.OverridePassword.Any((char c) => !char.IsLetter(c)))
			{
				return true;
			}
			ServerConsole.AddLog("Override password refused, because it must contain digit or special symbol (requirement for verified servers only).", ConsoleColor.Gray, false);
			return false;
		}
	}

	public UserGroup GetUserGroup(string userId)
	{
		if (!string.IsNullOrEmpty(userId) && this.Members.ContainsKey(userId))
		{
			return this.Groups[this.Members[userId]];
		}
		return null;
	}

	public ulong FullPerm { get; private set; }

	public bool StaffAccess { get; }

	public bool ManagersAccess
	{
		get
		{
			return this._managerAccess || this.StaffAccess || CustomNetworkManager.IsVerified;
		}
	}

	public bool BanningTeamAccess
	{
		get
		{
			return this._banTeamAccess || this.StaffAccess || CustomNetworkManager.IsVerified;
		}
	}

	public bool NorthwoodAccess { get; }

	public readonly Dictionary<string, UserGroup> Groups;

	public readonly Dictionary<string, string> Members;

	public readonly Dictionary<string, ulong> Permissions;

	internal readonly string OverridePassword;

	private readonly string _overrideRole;

	private readonly HashSet<ulong> _raPermissions;

	private readonly YamlConfig _config;

	private readonly YamlConfig _sharedGroups;

	private ulong _lastPerm;

	private readonly bool _managerAccess;

	private readonly bool _banTeamAccess;

	private readonly bool _banTeamSlots;

	private readonly bool _banTeamGeoBypass;

	public static readonly Dictionary<PlayerPermissions, string> PermissionCodes = new Dictionary<PlayerPermissions, string>
	{
		{
			PlayerPermissions.KickingAndShortTermBanning,
			"BN1"
		},
		{
			PlayerPermissions.BanningUpToDay,
			"BN2"
		},
		{
			PlayerPermissions.LongTermBanning,
			"BN3"
		},
		{
			PlayerPermissions.ForceclassSelf,
			"FSE"
		},
		{
			PlayerPermissions.ForceclassToSpectator,
			"FSP"
		},
		{
			PlayerPermissions.ForceclassWithoutRestrictions,
			"FWR"
		},
		{
			PlayerPermissions.GivingItems,
			"GIV"
		},
		{
			PlayerPermissions.WarheadEvents,
			"EWA"
		},
		{
			PlayerPermissions.RespawnEvents,
			"ERE"
		},
		{
			PlayerPermissions.RoundEvents,
			"ERO"
		},
		{
			PlayerPermissions.SetGroup,
			"SGR"
		},
		{
			PlayerPermissions.GameplayData,
			"GMD"
		},
		{
			PlayerPermissions.Overwatch,
			"OVR"
		},
		{
			PlayerPermissions.FacilityManagement,
			"FCM"
		},
		{
			PlayerPermissions.PlayersManagement,
			"PLM"
		},
		{
			PlayerPermissions.PermissionsManagement,
			"PRM"
		},
		{
			PlayerPermissions.ServerConsoleCommands,
			"SCC"
		},
		{
			PlayerPermissions.ViewHiddenBadges,
			"VHB"
		},
		{
			PlayerPermissions.ServerConfigs,
			"CFG"
		},
		{
			PlayerPermissions.Broadcasting,
			"BRC"
		},
		{
			PlayerPermissions.PlayerSensitiveDataAccess,
			"CDA"
		},
		{
			PlayerPermissions.Noclip,
			"NCP"
		},
		{
			PlayerPermissions.AFKImmunity,
			"AFK"
		},
		{
			PlayerPermissions.AdminChat,
			"ATC"
		},
		{
			PlayerPermissions.ViewHiddenGlobalBadges,
			"GHB"
		},
		{
			PlayerPermissions.Announcer,
			"ANN"
		},
		{
			PlayerPermissions.Effects,
			"EFF"
		},
		{
			PlayerPermissions.FriendlyFireDetectorImmunity,
			"FFI"
		},
		{
			PlayerPermissions.FriendlyFireDetectorTempDisable,
			"FFT"
		},
		{
			PlayerPermissions.ServerLogLiveFeed,
			"LLF"
		}
	};
}
