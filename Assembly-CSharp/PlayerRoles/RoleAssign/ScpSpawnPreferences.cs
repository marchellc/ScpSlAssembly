using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace PlayerRoles.RoleAssign
{
	public static class ScpSpawnPreferences
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				ScpSpawnPreferences.Preferences.Clear();
				NetworkServer.ReplaceHandler<ScpSpawnPreferences.SpawnPreferences>(new Action<NetworkConnectionToClient, ScpSpawnPreferences.SpawnPreferences>(ScpSpawnPreferences.OnMessageReceived), true);
				NetworkClient.Send<ScpSpawnPreferences.SpawnPreferences>(new ScpSpawnPreferences.SpawnPreferences(true), 0);
			};
		}

		private static int ClampPreference(int val)
		{
			return Mathf.Clamp(val, -5, 5);
		}

		private static void OnMessageReceived(NetworkConnection conn, ScpSpawnPreferences.SpawnPreferences msg)
		{
			ScpSpawnPreferences.Preferences[conn.connectionId] = msg;
		}

		public static int GetPreference(RoleTypeId role)
		{
			string text = "SpawnPreference_Role_";
			int num = (int)role;
			return ScpSpawnPreferences.ClampPreference(PlayerPrefsSl.Get(text + num.ToString(), 0));
		}

		public static void SavePreference(RoleTypeId role, int value)
		{
			string text = "SpawnPreference_Role_";
			int num = (int)role;
			PlayerPrefsSl.Set(text + num.ToString(), value);
			if (!NetworkClient.active)
			{
				return;
			}
			NetworkClient.Send<ScpSpawnPreferences.SpawnPreferences>(new ScpSpawnPreferences.SpawnPreferences(true), 0);
		}

		public static void WriteSpawnPreferences(this NetworkWriter writer, ScpSpawnPreferences.SpawnPreferences msg)
		{
			msg.Serialize(writer);
		}

		public static ScpSpawnPreferences.SpawnPreferences ReadSpawnPreferences(this NetworkReader reader)
		{
			return new ScpSpawnPreferences.SpawnPreferences(reader);
		}

		public static readonly Dictionary<int, ScpSpawnPreferences.SpawnPreferences> Preferences = new Dictionary<int, ScpSpawnPreferences.SpawnPreferences>();

		public const int MaxPreference = 5;

		private const string PrefsPrefix = "SpawnPreference_Role_";

		public readonly struct SpawnPreferences : NetworkMessage
		{
			public SpawnPreferences(bool autoSetup)
			{
				this._count = 0;
				this.Preferences = new Dictionary<RoleTypeId, int>();
				if (!autoSetup)
				{
					return;
				}
				foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
				{
					if (keyValuePair.Value is ISpawnableScp)
					{
						this.Preferences[keyValuePair.Key] = ScpSpawnPreferences.GetPreference(keyValuePair.Key);
						this._count += 1;
					}
				}
			}

			public SpawnPreferences(NetworkReader reader)
			{
				this._count = reader.ReadByte();
				this.Preferences = new Dictionary<RoleTypeId, int>((int)this._count);
				for (int i = 0; i < (int)this._count; i++)
				{
					RoleTypeId roleTypeId = reader.ReadRoleType();
					int num = (int)reader.ReadSByte();
					this.Preferences[roleTypeId] = ScpSpawnPreferences.ClampPreference(num);
				}
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteByte(this._count);
				foreach (KeyValuePair<RoleTypeId, int> keyValuePair in this.Preferences)
				{
					writer.WriteRoleType(keyValuePair.Key);
					writer.WriteSByte((sbyte)keyValuePair.Value);
				}
			}

			private readonly byte _count;

			public readonly Dictionary<RoleTypeId, int> Preferences;
		}
	}
}
