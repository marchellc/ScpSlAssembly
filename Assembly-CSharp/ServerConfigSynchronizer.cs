using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GameCore;
using InventorySystem.Configs;
using Mirror;
using UnityEngine;

public class ServerConfigSynchronizer : NetworkBehaviour
{
	public static void RefreshAllConfigs()
	{
		if (ServerConfigSynchronizer.Singleton == null)
		{
			return;
		}
		ServerConfigSynchronizer.Singleton.RefreshMainBools();
		ServerConfigSynchronizer.Singleton.RefreshCategoryLimits();
		ServerConfigSynchronizer.Singleton.RefreshAmmoLimits();
		ServerConfigSynchronizer.Singleton.RefreshRAConfigs();
		Action onRefreshed = ServerConfigSynchronizer.OnRefreshed;
		if (onRefreshed == null)
		{
			return;
		}
		onRefreshed();
	}

	[Server]
	public void RefreshMainBools()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshMainBools()' called when server was not active");
			return;
		}
		this.NetworkMainBoolsSync = Misc.BoolsToByte(ServerConsole.FriendlyFire, false, false, false, false, false, false, false);
	}

	[Server]
	public void RefreshRAConfigs()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshRAConfigs()' called when server was not active");
			return;
		}
		this.NetworkEnableRemoteAdminPredefinedBanTemplates = ServerStatic.RolesConfig.GetBool("enable_predefined_ban_templates", true);
		this.NetworkRemoteAdminExternalPlayerLookupMode = ServerStatic.RolesConfig.GetString("external_player_lookup_mode", "disabled").Trim().ToLower();
		this.NetworkRemoteAdminExternalPlayerLookupURL = ServerStatic.RolesConfig.GetString("external_player_lookup_url", "");
		this.RemoteAdminExternalPlayerLookupToken = ServerStatic.RolesConfig.GetString("external_player_lookup_token", "");
		this.RemoteAdminPredefinedBanTemplates.Clear();
		if (!this.EnableRemoteAdminPredefinedBanTemplates)
		{
			return;
		}
		List<string> stringList = ServerStatic.RolesConfig.GetStringList("PredefinedBanTemplates");
		if (stringList != null)
		{
			foreach (string text in stringList)
			{
				string[] array = YamlConfig.ParseCommaSeparatedString(text);
				int num;
				if (array.Length != 2)
				{
					ServerConsole.AddLog("Invalid ban template in RA Config file! Template: " + text, ConsoleColor.Gray, false);
				}
				else if (!int.TryParse(array[0], out num) || num < 0)
				{
					ServerConsole.AddLog("Invalid ban template in RA Config file - duration must be a non-negative integer. Ban template name: " + text, ConsoleColor.Gray, false);
				}
				else
				{
					ServerConfigSynchronizer.PredefinedBanTemplate predefinedBanTemplate;
					predefinedBanTemplate.Reason = array[1];
					TimeSpan timeSpan = TimeSpan.FromSeconds((double)num);
					predefinedBanTemplate.Duration = (int)timeSpan.TotalMinutes;
					int num2 = timeSpan.Days / 365;
					if (num2 > 0)
					{
						predefinedBanTemplate.FormattedDuration = string.Format("{0}y", num2);
					}
					else if (timeSpan.Days > 0)
					{
						predefinedBanTemplate.FormattedDuration = string.Format("{0}d", timeSpan.Days);
					}
					else if (timeSpan.Hours > 0)
					{
						predefinedBanTemplate.FormattedDuration = string.Format("{0}h", timeSpan.Hours);
					}
					else if (timeSpan.Minutes > 0)
					{
						predefinedBanTemplate.FormattedDuration = string.Format("{0}m", timeSpan.Minutes);
					}
					else
					{
						predefinedBanTemplate.FormattedDuration = string.Format("{0}s", timeSpan.Seconds);
					}
					this.RemoteAdminPredefinedBanTemplates.Add(predefinedBanTemplate);
				}
			}
			if (this.RemoteAdminPredefinedBanTemplates.Count == 0)
			{
				this.NetworkEnableRemoteAdminPredefinedBanTemplates = false;
				return;
			}
		}
		else
		{
			this.NetworkEnableRemoteAdminPredefinedBanTemplates = false;
		}
	}

	[Server]
	public void RefreshCategoryLimits()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshCategoryLimits()' called when server was not active");
			return;
		}
		this.CategoryLimits.Clear();
		int num = 0;
		while (Enum.IsDefined(typeof(ItemCategory), (ItemCategory)num))
		{
			ItemCategory itemCategory = (ItemCategory)num;
			sbyte b;
			if (InventoryLimits.StandardCategoryLimits.TryGetValue(itemCategory, out b) && b >= 0)
			{
				this.CategoryLimits.Add(ConfigFile.ServerConfig.GetSByte("limit_category_" + itemCategory.ToString().ToLowerInvariant(), b));
			}
			num++;
		}
	}

	[Server]
	public void RefreshAmmoLimits()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshAmmoLimits()' called when server was not active");
			return;
		}
		if (this.AmmoLimitsSync.Count > 0)
		{
			this.AmmoLimitsSync.Clear();
		}
		foreach (KeyValuePair<ItemType, ushort> keyValuePair in InventoryLimits.StandardAmmoLimits)
		{
			ushort @ushort = ConfigFile.ServerConfig.GetUShort("limit_" + keyValuePair.Key.ToString().ToLowerInvariant(), keyValuePair.Value);
			this.AmmoLimitsSync.Add(new ServerConfigSynchronizer.AmmoLimit
			{
				AmmoType = keyValuePair.Key,
				Limit = @ushort
			});
		}
	}

	private void Awake()
	{
		ServerConfigSynchronizer.Singleton = this;
	}

	private void Update()
	{
		if (this._ready)
		{
			return;
		}
		if (!NetworkServer.active)
		{
			this._ready = true;
			return;
		}
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHostHub(out referenceHub))
		{
			return;
		}
		this._ready = true;
		ServerConfigSynchronizer.RefreshAllConfigs();
	}

	public ServerConfigSynchronizer()
	{
		base.InitSyncObject(this.CategoryLimits);
		base.InitSyncObject(this.AmmoLimitsSync);
		base.InitSyncObject(this.RemoteAdminPredefinedBanTemplates);
	}

	public override bool Weaved()
	{
		return true;
	}

	public byte NetworkMainBoolsSync
	{
		get
		{
			return this.MainBoolsSync;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<byte>(value, ref this.MainBoolsSync, 1UL, null);
		}
	}

	public string NetworkServerName
	{
		get
		{
			return this.ServerName;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.ServerName, 2UL, null);
		}
	}

	public bool NetworkEnableRemoteAdminPredefinedBanTemplates
	{
		get
		{
			return this.EnableRemoteAdminPredefinedBanTemplates;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.EnableRemoteAdminPredefinedBanTemplates, 4UL, null);
		}
	}

	public string NetworkRemoteAdminExternalPlayerLookupMode
	{
		get
		{
			return this.RemoteAdminExternalPlayerLookupMode;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.RemoteAdminExternalPlayerLookupMode, 8UL, null);
		}
	}

	public string NetworkRemoteAdminExternalPlayerLookupURL
	{
		get
		{
			return this.RemoteAdminExternalPlayerLookupURL;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.RemoteAdminExternalPlayerLookupURL, 16UL, null);
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteByte(this.MainBoolsSync);
			writer.WriteString(this.ServerName);
			writer.WriteBool(this.EnableRemoteAdminPredefinedBanTemplates);
			writer.WriteString(this.RemoteAdminExternalPlayerLookupMode);
			writer.WriteString(this.RemoteAdminExternalPlayerLookupURL);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteByte(this.MainBoolsSync);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteString(this.ServerName);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this.EnableRemoteAdminPredefinedBanTemplates);
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteString(this.RemoteAdminExternalPlayerLookupMode);
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteString(this.RemoteAdminExternalPlayerLookupURL);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this.MainBoolsSync, null, reader.ReadByte());
			base.GeneratedSyncVarDeserialize<string>(ref this.ServerName, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize<bool>(ref this.EnableRemoteAdminPredefinedBanTemplates, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize<string>(ref this.RemoteAdminExternalPlayerLookupMode, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize<string>(ref this.RemoteAdminExternalPlayerLookupURL, null, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this.MainBoolsSync, null, reader.ReadByte());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.ServerName, null, reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.EnableRemoteAdminPredefinedBanTemplates, null, reader.ReadBool());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.RemoteAdminExternalPlayerLookupMode, null, reader.ReadString());
		}
		if ((num & 16L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.RemoteAdminExternalPlayerLookupURL, null, reader.ReadString());
		}
	}

	public static ServerConfigSynchronizer Singleton;

	public static Action OnRefreshed;

	[SyncVar]
	public byte MainBoolsSync;

	public SyncList<sbyte> CategoryLimits = new SyncList<sbyte>();

	public SyncList<ServerConfigSynchronizer.AmmoLimit> AmmoLimitsSync = new SyncList<ServerConfigSynchronizer.AmmoLimit>();

	[SyncVar]
	public string ServerName;

	[SyncVar]
	public bool EnableRemoteAdminPredefinedBanTemplates = true;

	[SyncVar]
	public string RemoteAdminExternalPlayerLookupMode = "disabled";

	[SyncVar]
	public string RemoteAdminExternalPlayerLookupURL = "";

	[NonSerialized]
	public string RemoteAdminExternalPlayerLookupToken = string.Empty;

	public readonly SyncList<ServerConfigSynchronizer.PredefinedBanTemplate> RemoteAdminPredefinedBanTemplates = new SyncList<ServerConfigSynchronizer.PredefinedBanTemplate>();

	private bool _ready;

	public enum MainBoolsSettings : byte
	{
		FriendlyFire = 1
	}

	[Serializable]
	public struct AmmoLimit
	{
		public ItemType AmmoType;

		public ushort Limit;
	}

	public struct PredefinedBanTemplate
	{
		public int Duration;

		public string FormattedDuration;

		public string Reason;
	}
}
