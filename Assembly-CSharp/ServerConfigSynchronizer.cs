using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GameCore;
using InventorySystem.Configs;
using Mirror;
using UnityEngine;

public class ServerConfigSynchronizer : NetworkBehaviour
{
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

	public static ServerConfigSynchronizer Singleton;

	public static Action OnRefreshed;

	[SyncVar]
	public byte MainBoolsSync;

	public SyncList<sbyte> CategoryLimits = new SyncList<sbyte>();

	public SyncList<AmmoLimit> AmmoLimitsSync = new SyncList<AmmoLimit>();

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

	public readonly SyncList<PredefinedBanTemplate> RemoteAdminPredefinedBanTemplates = new SyncList<PredefinedBanTemplate>();

	private bool _ready;

	public byte NetworkMainBoolsSync
	{
		get
		{
			return this.MainBoolsSync;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.MainBoolsSync, 1uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this.ServerName, 2uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this.EnableRemoteAdminPredefinedBanTemplates, 4uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this.RemoteAdminExternalPlayerLookupMode, 8uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this.RemoteAdminExternalPlayerLookupURL, 16uL, null);
		}
	}

	public static void RefreshAllConfigs()
	{
		if (!(ServerConfigSynchronizer.Singleton == null))
		{
			ServerConfigSynchronizer.Singleton.RefreshMainBools();
			ServerConfigSynchronizer.Singleton.RefreshCategoryLimits();
			ServerConfigSynchronizer.Singleton.RefreshAmmoLimits();
			ServerConfigSynchronizer.Singleton.RefreshRAConfigs();
			ServerConfigSynchronizer.OnRefreshed?.Invoke();
		}
	}

	[Server]
	public void RefreshMainBools()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshMainBools()' called when server was not active");
		}
		else
		{
			this.NetworkMainBoolsSync = Misc.BoolsToByte(ServerConsole.FriendlyFire);
		}
	}

	[Server]
	public void RefreshRAConfigs()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void ServerConfigSynchronizer::RefreshRAConfigs()' called when server was not active");
			return;
		}
		this.NetworkEnableRemoteAdminPredefinedBanTemplates = ServerStatic.RolesConfig.GetBool("enable_predefined_ban_templates", def: true);
		this.NetworkRemoteAdminExternalPlayerLookupMode = ServerStatic.RolesConfig.GetString("external_player_lookup_mode", "disabled").Trim().ToLower();
		this.NetworkRemoteAdminExternalPlayerLookupURL = ServerStatic.RolesConfig.GetString("external_player_lookup_url");
		this.RemoteAdminExternalPlayerLookupToken = ServerStatic.RolesConfig.GetString("external_player_lookup_token");
		this.RemoteAdminPredefinedBanTemplates.Clear();
		if (!this.EnableRemoteAdminPredefinedBanTemplates)
		{
			return;
		}
		List<string> stringList = ServerStatic.RolesConfig.GetStringList("PredefinedBanTemplates");
		if (stringList != null)
		{
			PredefinedBanTemplate item = default(PredefinedBanTemplate);
			foreach (string item2 in stringList)
			{
				string[] array = YamlConfig.ParseCommaSeparatedString(item2);
				if (array.Length != 2)
				{
					ServerConsole.AddLog("Invalid ban template in RA Config file! Template: " + item2);
					continue;
				}
				if (!int.TryParse(array[0], out var result) || result < 0)
				{
					ServerConsole.AddLog("Invalid ban template in RA Config file - duration must be a non-negative integer. Ban template name: " + item2);
					continue;
				}
				item.Reason = array[1];
				TimeSpan timeSpan = TimeSpan.FromSeconds(result);
				item.Duration = (int)timeSpan.TotalMinutes;
				int num = timeSpan.Days / 365;
				if (num > 0)
				{
					item.FormattedDuration = $"{num}y";
				}
				else if (timeSpan.Days > 0)
				{
					item.FormattedDuration = $"{timeSpan.Days}d";
				}
				else if (timeSpan.Hours > 0)
				{
					item.FormattedDuration = $"{timeSpan.Hours}h";
				}
				else if (timeSpan.Minutes > 0)
				{
					item.FormattedDuration = $"{timeSpan.Minutes}m";
				}
				else
				{
					item.FormattedDuration = $"{timeSpan.Seconds}s";
				}
				this.RemoteAdminPredefinedBanTemplates.Add(item);
			}
			if (this.RemoteAdminPredefinedBanTemplates.Count == 0)
			{
				this.NetworkEnableRemoteAdminPredefinedBanTemplates = false;
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
		for (int i = 0; Enum.IsDefined(typeof(ItemCategory), (ItemCategory)i); i++)
		{
			ItemCategory key = (ItemCategory)i;
			if (InventoryLimits.StandardCategoryLimits.TryGetValue(key, out var value) && value >= 0)
			{
				this.CategoryLimits.Add(ConfigFile.ServerConfig.GetSByte("limit_category_" + key.ToString().ToLowerInvariant(), value));
			}
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
		foreach (KeyValuePair<ItemType, ushort> standardAmmoLimit in InventoryLimits.StandardAmmoLimits)
		{
			ushort uShort = ConfigFile.ServerConfig.GetUShort("limit_" + standardAmmoLimit.Key.ToString().ToLowerInvariant(), standardAmmoLimit.Value);
			this.AmmoLimitsSync.Add(new AmmoLimit
			{
				AmmoType = standardAmmoLimit.Key,
				Limit = uShort
			});
		}
	}

	private void Awake()
	{
		ServerConfigSynchronizer.Singleton = this;
	}

	private void Update()
	{
		if (!this._ready)
		{
			ReferenceHub hub;
			if (!NetworkServer.active)
			{
				this._ready = true;
			}
			else if (ReferenceHub.TryGetHostHub(out hub))
			{
				this._ready = true;
				ServerConfigSynchronizer.RefreshAllConfigs();
			}
		}
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

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this.MainBoolsSync);
			writer.WriteString(this.ServerName);
			writer.WriteBool(this.EnableRemoteAdminPredefinedBanTemplates);
			writer.WriteString(this.RemoteAdminExternalPlayerLookupMode);
			writer.WriteString(this.RemoteAdminExternalPlayerLookupURL);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this.MainBoolsSync);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteString(this.ServerName);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteBool(this.EnableRemoteAdminPredefinedBanTemplates);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteString(this.RemoteAdminExternalPlayerLookupMode);
		}
		if ((base.syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteString(this.RemoteAdminExternalPlayerLookupURL);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.MainBoolsSync, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this.ServerName, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this.EnableRemoteAdminPredefinedBanTemplates, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this.RemoteAdminExternalPlayerLookupMode, null, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this.RemoteAdminExternalPlayerLookupURL, null, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.MainBoolsSync, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ServerName, null, reader.ReadString());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.EnableRemoteAdminPredefinedBanTemplates, null, reader.ReadBool());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.RemoteAdminExternalPlayerLookupMode, null, reader.ReadString());
		}
		if ((num & 0x10L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.RemoteAdminExternalPlayerLookupURL, null, reader.ReadString());
		}
	}
}
