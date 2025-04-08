using System;
using System.Collections.Generic;
using CentralAuth;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib.Pools;
using TMPro;
using UnityEngine;
using Utils.Networking;

namespace UserSettings.ServerSpecific
{
	public static class ServerSpecificSettingsSync
	{
		public static ServerSpecificSettingBase[] DefinedSettings { get; set; }

		public static Predicate<ReferenceHub> SendOnJoinFilter { get; set; }

		public static event Action<ReferenceHub, ServerSpecificSettingBase> ServerOnSettingValueReceived;

		public static event Action<ReferenceHub, SSSUserStatusReport> ServerOnStatusReceived;

		public static string CurServerPrefsKey
		{
			get
			{
				string serverIDLastJoined = FavoriteAndHistory.ServerIDLastJoined;
				if (string.IsNullOrEmpty(serverIDLastJoined))
				{
					return LiteNetLib4MirrorNetworkManager.singleton.networkAddress;
				}
				return serverIDLastJoined;
			}
		}

		private static Type[] AllSettingTypes
		{
			get
			{
				if (ServerSpecificSettingsSync._allTypes != null)
				{
					return ServerSpecificSettingsSync._allTypes;
				}
				ServerSpecificSettingsSync._allTypes = new Type[ServerSpecificSettingsSync.AllSettingConstructors.Length];
				for (int i = 0; i < ServerSpecificSettingsSync._allTypes.Length; i++)
				{
					ServerSpecificSettingsSync._allTypes[i] = ServerSpecificSettingsSync.AllSettingConstructors[i]().GetType();
				}
				return ServerSpecificSettingsSync._allTypes;
			}
		}

		public static T GetSettingOfUser<T>(ReferenceHub user, int id) where T : ServerSpecificSettingBase
		{
			T t;
			if (ServerSpecificSettingsSync.TryGetSettingOfUser<T>(user, id, out t))
			{
				return t;
			}
			T t2 = ServerSpecificSettingsSync.CreateInstance(typeof(T)) as T;
			t2.SetId(new int?(id), null);
			t2.ApplyDefaultValues();
			ServerSpecificSettingsSync.ReceivedUserSettings[user].Add(t2);
			return t2;
		}

		public static bool TryGetSettingOfUser<T>(ReferenceHub user, int id, out T result) where T : ServerSpecificSettingBase
		{
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in ServerSpecificSettingsSync.ReceivedUserSettings.GetOrAdd(user, () => new List<ServerSpecificSettingBase>()))
			{
				if (serverSpecificSettingBase.SettingId == id)
				{
					T t = serverSpecificSettingBase as T;
					if (t != null)
					{
						result = t;
						return true;
					}
				}
			}
			result = default(T);
			return false;
		}

		public static int GetUserVersion(ReferenceHub user)
		{
			SSSUserStatusReport sssuserStatusReport;
			if (!ServerSpecificSettingsSync.ReceivedUserStatuses.TryGetValue(user, out sssuserStatusReport))
			{
				return 0;
			}
			return sssuserStatusReport.Version;
		}

		public static bool IsTabOpenForUser(ReferenceHub user)
		{
			SSSUserStatusReport sssuserStatusReport;
			return ServerSpecificSettingsSync.ReceivedUserStatuses.TryGetValue(user, out sssuserStatusReport) && sssuserStatusReport.TabOpen;
		}

		public static byte GetCodeFromType(Type type)
		{
			int num = ServerSpecificSettingsSync.AllSettingTypes.IndexOf(type);
			if (num < 0)
			{
				throw new ArgumentException(type.FullName + " is not a supported server-specific setting serializer.", "type");
			}
			return (byte)num;
		}

		public static Type GetTypeFromCode(byte header)
		{
			return ServerSpecificSettingsSync.AllSettingTypes[(int)header];
		}

		public static void SendToAll()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version).SendToAuthenticated(0);
		}

		public static void SendToPlayersConditionally(Func<ReferenceHub, bool> filter)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version).SendToHubsConditionally(filter, 0);
		}

		public static void SendToPlayer(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			hub.connectionToClient.Send<SSSEntriesPack>(new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version), 0);
		}

		public static void SendToPlayer(ReferenceHub hub, ServerSpecificSettingBase[] collection, int? versionOverride = null)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			hub.connectionToClient.Send<SSSEntriesPack>(new SSSEntriesPack(collection, versionOverride ?? ServerSpecificSettingsSync.Version), 0);
		}

		public static ServerSpecificSettingBase CreateInstance(Type t)
		{
			return ServerSpecificSettingsSync.AllSettingConstructors[ServerSpecificSettingsSync.AllSettingTypes.IndexOf(t)]();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerAuthenticationManager.OnInstanceModeChanged += delegate(ReferenceHub hub, ClientInstanceMode _)
			{
				if (ServerSpecificSettingsSync.SendOnJoinFilter != null && !ServerSpecificSettingsSync.SendOnJoinFilter(hub))
				{
					return;
				}
				ServerSpecificSettingsSync.SendToPlayer(hub);
			};
			CustomNetworkManager.OnClientReady += delegate
			{
				ServerSpecificSettingsSync.ReceivedUserSettings.Clear();
				ServerSpecificSettingsSync.ReceivedUserStatuses.Clear();
				NetworkClient.ReplaceHandler<SSSEntriesPack>(new Action<SSSEntriesPack>(ServerSpecificSettingsSync.ClientProcessPackMsg), true);
				NetworkClient.ReplaceHandler<SSSUpdateMessage>(new Action<SSSUpdateMessage>(ServerSpecificSettingsSync.ClientProcessUpdateMsg), true);
				NetworkServer.ReplaceHandler<SSSClientResponse>(new Action<NetworkConnectionToClient, SSSClientResponse>(ServerSpecificSettingsSync.ServerProcessClientResponseMsg), true);
				NetworkServer.ReplaceHandler<SSSUserStatusReport>(new Action<NetworkConnectionToClient, SSSUserStatusReport>(ServerSpecificSettingsSync.ServerProcessClientStatusMsg), true);
			};
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				ServerSpecificSettingsSync.ReceivedUserSettings.Remove(hub);
				ServerSpecificSettingsSync.ReceivedUserStatuses.Remove(hub);
			}));
			StaticUnityMethods.OnUpdate += ServerSpecificSettingsSync.UpdateDefinedSettings;
		}

		private static void UpdateDefinedSettings()
		{
			try
			{
				if (StaticUnityMethods.IsPlaying)
				{
					ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
					if (definedSettings != null)
					{
						definedSettings.ForEach(delegate(ServerSpecificSettingBase x)
						{
							x.OnUpdate();
						});
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private static void ClientProcessPackMsg(SSSEntriesPack pack)
		{
			ServerSpecificSettingsSync.DefinedSettings = pack.Settings;
			ServerSpecificSettingsSync.Version = pack.Version;
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in ServerSpecificSettingsSync.DefinedSettings)
			{
				if (serverSpecificSettingBase.ResponseMode == ServerSpecificSettingBase.UserResponseMode.AcquisitionAndChange)
				{
					serverSpecificSettingBase.ClientSendValue();
				}
			}
		}

		private static void ClientProcessUpdateMsg(SSSUpdateMessage msg)
		{
			Type typeFromCode = ServerSpecificSettingsSync.GetTypeFromCode(msg.TypeCode);
			List<byte> deserializedPooledPayload = msg.DeserializedPooledPayload;
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in ServerSpecificSettingsSync.DefinedSettings)
			{
				ISSUpdatable issupdatable = serverSpecificSettingBase as ISSUpdatable;
				if (issupdatable != null && serverSpecificSettingBase.SettingId == msg.Id && !(serverSpecificSettingBase.GetType() != typeFromCode))
				{
					if (ServerSpecificSettingsSync._payloadBufferNonAlloc.Length < deserializedPooledPayload.Count)
					{
						ServerSpecificSettingsSync._payloadBufferNonAlloc = new byte[deserializedPooledPayload.Count + ServerSpecificSettingsSync._payloadBufferNonAlloc.Length];
					}
					deserializedPooledPayload.CopyTo(ServerSpecificSettingsSync._payloadBufferNonAlloc);
					using (NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(new ArraySegment<byte>(ServerSpecificSettingsSync._payloadBufferNonAlloc, 0, deserializedPooledPayload.Count)))
					{
						issupdatable.DeserializeUpdate(networkReaderPooled);
						break;
					}
				}
			}
			ListPool<byte>.Shared.Return(deserializedPooledPayload);
		}

		private static bool ServerPrevalidateClientResponse(SSSClientResponse msg)
		{
			if (ServerSpecificSettingsSync.DefinedSettings == null)
			{
				return false;
			}
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in ServerSpecificSettingsSync.DefinedSettings)
			{
				if (serverSpecificSettingBase.SettingId == msg.Id && !(serverSpecificSettingBase.GetType() != msg.SettingType))
				{
					return true;
				}
			}
			return false;
		}

		private static void ServerDeserializeClientResponse(ReferenceHub sender, ServerSpecificSettingBase setting, NetworkReaderPooled reader)
		{
			if (setting.ResponseMode != ServerSpecificSettingBase.UserResponseMode.None)
			{
				setting.DeserializeValue(reader);
				Action<ReferenceHub, ServerSpecificSettingBase> serverOnSettingValueReceived = ServerSpecificSettingsSync.ServerOnSettingValueReceived;
				if (serverOnSettingValueReceived != null)
				{
					serverOnSettingValueReceived(sender, setting);
				}
			}
			reader.Dispose();
		}

		private static void ServerProcessClientResponseMsg(NetworkConnection conn, SSSClientResponse msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			if (!ServerSpecificSettingsSync.ServerPrevalidateClientResponse(msg))
			{
				return;
			}
			List<ServerSpecificSettingBase> orAdd = ServerSpecificSettingsSync.ReceivedUserSettings.GetOrAdd(referenceHub, () => new List<ServerSpecificSettingBase>());
			NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(msg.Payload);
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in orAdd)
			{
				if (serverSpecificSettingBase.SettingId == msg.Id && !(serverSpecificSettingBase.GetType() != msg.SettingType))
				{
					ServerSpecificSettingsSync.ServerDeserializeClientResponse(referenceHub, serverSpecificSettingBase, networkReaderPooled);
					return;
				}
			}
			ServerSpecificSettingBase serverSpecificSettingBase2 = ServerSpecificSettingsSync.CreateInstance(msg.SettingType);
			orAdd.Add(serverSpecificSettingBase2);
			serverSpecificSettingBase2.SetId(new int?(msg.Id), null);
			serverSpecificSettingBase2.ApplyDefaultValues();
			ServerSpecificSettingsSync.ServerDeserializeClientResponse(referenceHub, serverSpecificSettingBase2, networkReaderPooled);
		}

		private static void ServerProcessClientStatusMsg(NetworkConnection conn, SSSUserStatusReport msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			ServerSpecificSettingsSync.ReceivedUserStatuses[referenceHub] = msg;
			Action<ReferenceHub, SSSUserStatusReport> serverOnStatusReceived = ServerSpecificSettingsSync.ServerOnStatusReceived;
			if (serverOnStatusReceived == null)
			{
				return;
			}
			serverOnStatusReceived(referenceHub, msg);
		}

		public static int Version = 1;

		private static Type[] _allTypes;

		private static byte[] _payloadBufferNonAlloc = new byte[1500];

		private static readonly Dictionary<ReferenceHub, List<ServerSpecificSettingBase>> ReceivedUserSettings = new Dictionary<ReferenceHub, List<ServerSpecificSettingBase>>();

		private static readonly Dictionary<ReferenceHub, SSSUserStatusReport> ReceivedUserStatuses = new Dictionary<ReferenceHub, SSSUserStatusReport>();

		private static readonly Func<ServerSpecificSettingBase>[] AllSettingConstructors = new Func<ServerSpecificSettingBase>[]
		{
			() => new SSGroupHeader(null, false, null),
			() => new SSKeybindSetting(new int?(0), null, KeyCode.None, true, null),
			() => new SSDropdownSetting(new int?(0), null, null, 0, SSDropdownSetting.DropdownEntryType.Regular, null),
			() => new SSTwoButtonsSetting(new int?(0), null, null, null, false, null),
			() => new SSSliderSetting(new int?(0), null, 0f, 0f, 0f, false, "0.##", "{0}", null),
			() => new SSPlaintextSetting(new int?(0), null, "...", 64, TMP_InputField.ContentType.Standard, null),
			() => new SSButton(new int?(0), null, null, null, null),
			() => new SSTextArea(new int?(0), null, SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft)
		};
	}
}
