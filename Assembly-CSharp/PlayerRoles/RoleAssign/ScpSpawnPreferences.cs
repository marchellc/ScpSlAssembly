using System.Collections.Generic;
using Mirror;
using PlayerRoles.PlayableScps;
using UnityEngine;
using UserSettings;
using UserSettings.OtherSettings;

namespace PlayerRoles.RoleAssign;

public static class ScpSpawnPreferences
{
	public readonly struct SpawnPreferences : NetworkMessage
	{
		private readonly byte _count;

		public readonly Dictionary<RoleTypeId, int> Preferences;

		public readonly bool OptOutOfScp;

		public SpawnPreferences(bool autoSetup)
		{
			this._count = 0;
			this.Preferences = new Dictionary<RoleTypeId, int>();
			this.OptOutOfScp = false;
			if (!autoSetup)
			{
				return;
			}
			this.OptOutOfScp = UserSetting<bool>.Get(ScpSetting.ScpOptOut, defaultValue: false);
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> allRole in PlayerRoleLoader.AllRoles)
			{
				if (allRole.Value is ISpawnableScp)
				{
					this.Preferences[allRole.Key] = ScpSpawnPreferences.GetPreference(allRole.Key);
					this._count++;
				}
			}
		}

		public SpawnPreferences(NetworkReader reader)
		{
			this.OptOutOfScp = reader.ReadBool();
			this._count = reader.ReadByte();
			this.Preferences = new Dictionary<RoleTypeId, int>(this._count);
			for (int i = 0; i < this._count; i++)
			{
				RoleTypeId key = reader.ReadRoleType();
				int val = reader.ReadSByte();
				this.Preferences[key] = ScpSpawnPreferences.ClampPreference(val);
			}
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteBool(this.OptOutOfScp);
			writer.WriteByte(this._count);
			foreach (KeyValuePair<RoleTypeId, int> preference in this.Preferences)
			{
				writer.WriteRoleType(preference.Key);
				writer.WriteSByte((sbyte)preference.Value);
			}
		}
	}

	public static readonly Dictionary<int, SpawnPreferences> Preferences = new Dictionary<int, SpawnPreferences>();

	public const int MaxPreference = 5;

	private const string PrefsPrefix = "SpawnPreference_Role_";

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			ScpSpawnPreferences.Preferences.Clear();
			NetworkServer.ReplaceHandler<SpawnPreferences>(OnMessageReceived);
			NetworkClient.Send(new SpawnPreferences(autoSetup: true));
			UserSetting<bool>.AddListener(ScpSetting.ScpOptOut, delegate
			{
				NetworkClient.Send(new SpawnPreferences(autoSetup: true));
			});
		};
	}

	private static int ClampPreference(int val)
	{
		return Mathf.Clamp(val, -5, 5);
	}

	private static void OnMessageReceived(NetworkConnection conn, SpawnPreferences msg)
	{
		ScpSpawnPreferences.Preferences[conn.connectionId] = msg;
	}

	public static int GetPreference(RoleTypeId role)
	{
		int num = (int)role;
		return ScpSpawnPreferences.ClampPreference(PlayerPrefsSl.Get("SpawnPreference_Role_" + num, 0));
	}

	public static void SavePreference(RoleTypeId role, int value)
	{
		int num = (int)role;
		PlayerPrefsSl.Set("SpawnPreference_Role_" + num, value);
		if (NetworkClient.active)
		{
			NetworkClient.Send(new SpawnPreferences(autoSetup: true));
		}
	}

	public static void WriteSpawnPreferences(this NetworkWriter writer, SpawnPreferences msg)
	{
		msg.Serialize(writer);
	}

	public static SpawnPreferences ReadSpawnPreferences(this NetworkReader reader)
	{
		return new SpawnPreferences(reader);
	}
}
