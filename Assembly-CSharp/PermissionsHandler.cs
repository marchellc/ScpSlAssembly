using System;
using System.Collections.Generic;
using System.Linq;
using GameCore;

public class PermissionsHandler
{
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
		},
		{
			PlayerPermissions.ExecuteAs,
			"EXA"
		}
	};

	public bool BanTeamSlots
	{
		get
		{
			if (!this._banTeamSlots)
			{
				return CustomNetworkManager.IsVerified;
			}
			return true;
		}
	}

	public bool BanTeamBypassGeo
	{
		get
		{
			if (!this._banTeamGeoBypass)
			{
				return CustomNetworkManager.IsVerified;
			}
			return true;
		}
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
				ServerConsole.AddLog("Override password refused, because it's too short (requirement for verified servers only).");
				return false;
			}
			if (this.OverridePassword.ToLower() == this.OverridePassword || this.OverridePassword.ToUpper() == this.OverridePassword)
			{
				ServerConsole.AddLog("Override password refused, because it must contain mixed case chars (requirement for verified servers only).");
				return false;
			}
			if (this.OverridePassword.Any((char c) => !char.IsLetter(c)))
			{
				return true;
			}
			ServerConsole.AddLog("Override password refused, because it must contain digit or special symbol (requirement for verified servers only).");
			return false;
		}
	}

	public ulong FullPerm { get; private set; }

	public bool StaffAccess { get; }

	public bool ManagersAccess
	{
		get
		{
			if (!this._managerAccess && !this.StaffAccess)
			{
				return CustomNetworkManager.IsVerified;
			}
			return true;
		}
	}

	public bool BanningTeamAccess
	{
		get
		{
			if (!this._banTeamAccess && !this.StaffAccess)
			{
				return CustomNetworkManager.IsVerified;
			}
			return true;
		}
	}

	public bool NorthwoodAccess { get; }

	public PermissionsHandler(ref YamlConfig configuration, ref YamlConfig sharedGroups, ref YamlConfig sharedGroupsMembers)
	{
		this._config = new YamlConfig(configuration.Path);
		this._sharedGroups = sharedGroups;
		this.OverridePassword = configuration.GetString("override_password", "none");
		this._overrideRole = configuration.GetString("override_password_role", "owner");
		this.StaffAccess = configuration.GetBool("enable_staff_access");
		this._managerAccess = configuration.GetBool("enable_manager_access", def: true);
		this._banTeamAccess = configuration.GetBool("enable_banteam_access", def: true);
		this._banTeamSlots = configuration.GetBool("enable_banteam_reserved_slots", def: true);
		this._banTeamGeoBypass = configuration.GetBool("enable_banteam_bypass_geoblocking", def: true);
		this.NorthwoodAccess = configuration.GetBool("enable_northwood_access");
		if (this.NorthwoodAccess)
		{
			ServerConsole.AddLog("WARNING - Northwood staff access is enabled! All NW Studios staff members will have FULL Remote Admin access (this should only be used on testing servers)! You can disable this by setting 'enable_northwood_access' to false in your remote admin config file.", ConsoleColor.Yellow);
		}
		this.Groups = new Dictionary<string, UserGroup>();
		this._raPermissions = new HashSet<ulong>();
		List<string> stringList = configuration.GetStringList("Roles");
		List<string> stringList2 = configuration.GetStringList("Roles");
		if (sharedGroups != null)
		{
			List<string> stringList3 = sharedGroups.GetStringList("SharedRoles");
			string text = ConfigFile.SharingConfig.GetString("groups_sharing_mode").ToLowerInvariant();
			switch (text)
			{
			case "all":
				stringList.AddRange(stringList3);
				break;
			case "opt-in":
			{
				List<string> optIn = ConfigFile.SharingConfig.GetStringList("groups_opt_in_list");
				stringList.AddRange(stringList3.Where((string group) => optIn.Contains(group)));
				break;
			}
			case "opt-out":
			{
				List<string> optOut = ConfigFile.SharingConfig.GetStringList("groups_opt_out_list");
				stringList.AddRange(stringList3.Where((string group) => !optOut.Contains(group)));
				break;
			}
			default:
				ServerConsole.AddLog("Invalid group sharing mode set!");
				break;
			}
		}
		string[] array = configuration.GetKeys().ToArray();
		foreach (string item in stringList)
		{
			string text2 = ((array.Contains<string>(item + "_badge") || sharedGroups == null) ? configuration.GetString(item + "_badge") : sharedGroups.GetString(item + "_badge"));
			string text3 = ((array.Contains<string>(item + "_color") || sharedGroups == null) ? configuration.GetString(item + "_color") : sharedGroups.GetString(item + "_color"));
			bool cover = ((array.Contains<string>(item + "_cover") || sharedGroups == null) ? configuration.GetBool(item + "_cover", def: true) : sharedGroups.GetBool(item + "_cover", def: true));
			bool hiddenByDefault = ((array.Contains<string>(item + "_hidden") || sharedGroups == null) ? configuration.GetBool(item + "_hidden") : sharedGroups.GetBool(item + "_hidden"));
			byte kickPower = ((array.Contains<string>(item + "_kick_power") || sharedGroups == null) ? configuration.GetByte(item + "_kick_power", 0) : sharedGroups.GetByte(item + "_kick_power", 0));
			byte requiredKickPower = ((array.Contains<string>(item + "_required_kick_power") || sharedGroups == null) ? configuration.GetByte(item + "_required_kick_power", 0) : sharedGroups.GetByte(item + "_required_kick_power", 0));
			if (!(text2 == "") && !(text3 == ""))
			{
				if (this.Groups.ContainsKey(item))
				{
					ServerConsole.AddLog("Duplicated group definition: " + item + ".");
					continue;
				}
				this.Groups.Add(item, new UserGroup
				{
					Name = item,
					BadgeColor = text3,
					BadgeText = text2,
					Permissions = 0uL,
					Cover = cover,
					HiddenByDefault = hiddenByDefault,
					Shared = !stringList2.Contains(item),
					KickPower = kickPower,
					RequiredKickPower = requiredKickPower
				});
			}
		}
		this.Members = configuration.GetStringDictionary("Members");
		Dictionary<string, string> dictionary = sharedGroupsMembers?.GetStringDictionary("SharedMembers");
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				if (this.Members.TryGetValue(item2.Key, out var value))
				{
					ServerConsole.AddLog("Duplicated group member: " + item2.Key + ". Is member of " + value + " and " + item2.Value + ".");
				}
				else
				{
					this.Members.Add(item2.Key, item2.Value);
				}
			}
		}
		this._lastPerm = 1uL;
		HashSet<string> hashSet = new HashSet<string>();
		if (this.Members != null)
		{
			foreach (KeyValuePair<string, string> member in this.Members)
			{
				if (!this.Groups.ContainsKey(member.Value))
				{
					hashSet.Add(member.Key);
				}
			}
		}
		if (hashSet.Count > 0 && this.Members != null)
		{
			foreach (string item3 in hashSet)
			{
				this.Members.Remove(item3);
			}
		}
		hashSet.Clear();
		this.Permissions = new Dictionary<string, ulong>();
		string[] names = EnumUtils<PlayerPermissions>.Names;
		foreach (string text4 in names)
		{
			ulong num2 = (ulong)Enum.Parse(typeof(PlayerPermissions), text4);
			this.FullPerm |= num2;
			this.Permissions.Add(text4, num2);
			if (num2 != 4096 && num2 != 131072 && num2 != 2097152 && num2 != 4194304 && num2 != 16777216 && num2 != 134217728 && num2 != 8388608)
			{
				this._raPermissions.Add(num2);
			}
			if (num2 > this._lastPerm)
			{
				this._lastPerm = num2;
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
		foreach (KeyValuePair<string, UserGroup> group in this.Groups)
		{
			group.Value.Permissions = 0uL;
		}
		Dictionary<string, string> stringDictionary = this._config.GetStringDictionary("Permissions");
		Dictionary<string, string> dictionary = this._sharedGroups?.GetStringDictionary("SharedPermissions");
		foreach (string key3 in this.Permissions.Keys)
		{
			ulong num = this.Permissions[key3];
			if (stringDictionary.TryGetValue(key3, out var value))
			{
				string[] array = YamlConfig.ParseCommaSeparatedString(value);
				if (array == null)
				{
					ServerConsole.AddLog("Failed to process group permissions in remote admin config! Make sure there is no typo.");
				}
				else
				{
					string[] array2 = array;
					foreach (string key in array2)
					{
						if (this.Groups.TryGetValue(key, out var value2))
						{
							value2.Permissions |= num;
						}
					}
				}
			}
			else
			{
				ServerConsole.AddLog("RemoteAdmin config is missing permission definition: " + key3);
			}
			if (dictionary == null)
			{
				continue;
			}
			if (dictionary.TryGetValue(key3, out var value3))
			{
				string[] array3 = YamlConfig.ParseCommaSeparatedString(value3);
				if (array3 == null)
				{
					continue;
				}
				string[] array2 = array3;
				foreach (string key2 in array2)
				{
					if (this.Groups.TryGetValue(key2, out var value4))
					{
						value4.Permissions |= num;
					}
				}
			}
			else
			{
				ServerConsole.AddLog("Shared groups config is missing permission definition: " + key3);
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
		return this.Groups.Keys.ToList();
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
		return this.Permissions.Keys.ToList();
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
		ulong num = 0uL;
		num = check.Aggregate(0uL, (ulong current, PlayerPermissions c) => current | (ulong)c);
		return PermissionsHandler.IsPermitted(permissions, num);
	}

	public bool IsPermitted(ulong permissions, string check)
	{
		if (this.Permissions.ContainsKey(check))
		{
			return PermissionsHandler.IsPermitted(permissions, this.Permissions[check]);
		}
		return false;
	}

	public bool IsPermitted(ulong permissions, string[] check)
	{
		if (check.Length == 0)
		{
			return true;
		}
		ulong check2 = check.Where((string c) => this.Permissions.ContainsKey(c)).Aggregate(0uL, (ulong current, string c) => current | this.Permissions[c]);
		return PermissionsHandler.IsPermitted(permissions, check2);
	}

	public static bool IsPermitted(ulong permissions, ulong check)
	{
		return (permissions & check) != 0;
	}

	public UserGroup GetUserGroup(string userId)
	{
		if (!string.IsNullOrEmpty(userId) && this.Members.ContainsKey(userId))
		{
			return this.Groups[this.Members[userId]];
		}
		return null;
	}
}
