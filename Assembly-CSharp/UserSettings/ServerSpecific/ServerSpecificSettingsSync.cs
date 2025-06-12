using System;
using System.Collections.Generic;
using CentralAuth;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib.Pools;
using UnityEngine;
using Utils.Networking;

namespace UserSettings.ServerSpecific;

public static class ServerSpecificSettingsSync
{
	public static int Version = 1;

	private static Type[] _allTypes;

	private static byte[] _payloadBufferNonAlloc = new byte[1500];

	private static readonly Dictionary<ReferenceHub, List<ServerSpecificSettingBase>> ReceivedUserSettings = new Dictionary<ReferenceHub, List<ServerSpecificSettingBase>>();

	private static readonly Dictionary<ReferenceHub, SSSUserStatusReport> ReceivedUserStatuses = new Dictionary<ReferenceHub, SSSUserStatusReport>();

	private static readonly Func<ServerSpecificSettingBase>[] AllSettingConstructors = new Func<ServerSpecificSettingBase>[8]
	{
		() => new SSGroupHeader(null),
		() => new SSKeybindSetting(0, null),
		() => new SSDropdownSetting(0, null, null),
		() => new SSTwoButtonsSetting(0, null, null, null),
		() => new SSSliderSetting(0, null, 0f, 0f),
		() => new SSPlaintextSetting(0, null),
		() => new SSButton(0, null, null),
		() => new SSTextArea(0, null)
	};

	public static ServerSpecificSettingBase[] DefinedSettings { get; set; }

	public static Predicate<ReferenceHub> SendOnJoinFilter { get; set; }

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

	public static event Action<ReferenceHub, ServerSpecificSettingBase> ServerOnSettingValueReceived;

	public static event Action<ReferenceHub, SSSUserStatusReport> ServerOnStatusReceived;

	public static T GetSettingOfUser<T>(ReferenceHub user, int id) where T : ServerSpecificSettingBase
	{
		if (ServerSpecificSettingsSync.TryGetSettingOfUser<T>(user, id, out var result))
		{
			return result;
		}
		T val = ServerSpecificSettingsSync.CreateInstance(typeof(T)) as T;
		val.SetId(id, null);
		val.ApplyDefaultValues();
		ServerSpecificSettingsSync.ReceivedUserSettings[user].Add(val);
		return val;
	}

	public static bool TryGetSettingOfUser<T>(ReferenceHub user, int id, out T result) where T : ServerSpecificSettingBase
	{
		foreach (ServerSpecificSettingBase item in ServerSpecificSettingsSync.ReceivedUserSettings.GetOrAddNew(user))
		{
			if (item.SettingId == id && item is T val)
			{
				result = val;
				return true;
			}
		}
		result = null;
		return false;
	}

	public static int GetUserVersion(ReferenceHub user)
	{
		if (!ServerSpecificSettingsSync.ReceivedUserStatuses.TryGetValue(user, out var value))
		{
			return 0;
		}
		return value.Version;
	}

	public static bool IsTabOpenForUser(ReferenceHub user)
	{
		if (ServerSpecificSettingsSync.ReceivedUserStatuses.TryGetValue(user, out var value))
		{
			return value.TabOpen;
		}
		return false;
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
		return ServerSpecificSettingsSync.AllSettingTypes[header];
	}

	public static void SendToAll()
	{
		if (NetworkServer.active)
		{
			new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version).SendToAuthenticated();
		}
	}

	public static void SendToPlayersConditionally(Func<ReferenceHub, bool> filter)
	{
		if (NetworkServer.active)
		{
			new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version).SendToHubsConditionally(filter);
		}
	}

	public static void SendToPlayer(ReferenceHub hub)
	{
		if (NetworkServer.active)
		{
			hub.connectionToClient.Send(new SSSEntriesPack(ServerSpecificSettingsSync.DefinedSettings, ServerSpecificSettingsSync.Version));
		}
	}

	public static void SendToPlayer(ReferenceHub hub, ServerSpecificSettingBase[] collection, int? versionOverride = null)
	{
		if (NetworkServer.active)
		{
			hub.connectionToClient.Send(new SSSEntriesPack(collection, versionOverride ?? ServerSpecificSettingsSync.Version));
		}
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
			if (ServerSpecificSettingsSync.SendOnJoinFilter == null || ServerSpecificSettingsSync.SendOnJoinFilter(hub))
			{
				ServerSpecificSettingsSync.SendToPlayer(hub);
			}
		};
		CustomNetworkManager.OnClientReady += delegate
		{
			ServerSpecificSettingsSync.ReceivedUserSettings.Clear();
			ServerSpecificSettingsSync.ReceivedUserStatuses.Clear();
			NetworkClient.ReplaceHandler<SSSEntriesPack>(ClientProcessPackMsg);
			NetworkClient.ReplaceHandler<SSSUpdateMessage>(ClientProcessUpdateMsg);
			NetworkServer.ReplaceHandler<SSSClientResponse>(ServerProcessClientResponseMsg);
			NetworkServer.ReplaceHandler<SSSUserStatusReport>(ServerProcessClientStatusMsg);
		};
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			if (NetworkServer.active)
			{
				ServerSpecificSettingsSync.ReceivedUserSettings.Remove(hub);
				ServerSpecificSettingsSync.ReceivedUserStatuses.Remove(hub);
			}
		};
		StaticUnityMethods.OnUpdate += UpdateDefinedSettings;
	}

	private static void UpdateDefinedSettings()
	{
		try
		{
			if (StaticUnityMethods.IsPlaying)
			{
				ServerSpecificSettingsSync.DefinedSettings?.ForEach(delegate(ServerSpecificSettingBase x)
				{
					x.OnUpdate();
				});
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private static void ClientProcessPackMsg(SSSEntriesPack pack)
	{
	}

	private static void ClientProcessUpdateMsg(SSSUpdateMessage msg)
	{
		Type typeFromCode = ServerSpecificSettingsSync.GetTypeFromCode(msg.TypeCode);
		List<byte> deserializedPooledPayload = msg.DeserializedPooledPayload;
		ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
		foreach (ServerSpecificSettingBase serverSpecificSettingBase in definedSettings)
		{
			if (serverSpecificSettingBase is ISSUpdatable iSSUpdatable && serverSpecificSettingBase.SettingId == msg.Id && !(serverSpecificSettingBase.GetType() != typeFromCode))
			{
				if (ServerSpecificSettingsSync._payloadBufferNonAlloc.Length < deserializedPooledPayload.Count)
				{
					ServerSpecificSettingsSync._payloadBufferNonAlloc = new byte[deserializedPooledPayload.Count + ServerSpecificSettingsSync._payloadBufferNonAlloc.Length];
				}
				deserializedPooledPayload.CopyTo(ServerSpecificSettingsSync._payloadBufferNonAlloc);
				using (NetworkReaderPooled reader = NetworkReaderPool.Get(new ArraySegment<byte>(ServerSpecificSettingsSync._payloadBufferNonAlloc, 0, deserializedPooledPayload.Count)))
				{
					iSSUpdatable.DeserializeUpdate(reader);
				}
				break;
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
		ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
		foreach (ServerSpecificSettingBase serverSpecificSettingBase in definedSettings)
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
			ServerSpecificSettingsSync.ServerOnSettingValueReceived?.Invoke(sender, setting);
		}
		reader.Dispose();
	}

	private static void ServerProcessClientResponseMsg(NetworkConnection conn, SSSClientResponse msg)
	{
		if (!ReferenceHub.TryGetHub(conn, out var hub) || !ServerSpecificSettingsSync.ServerPrevalidateClientResponse(msg))
		{
			return;
		}
		List<ServerSpecificSettingBase> orAddNew = ServerSpecificSettingsSync.ReceivedUserSettings.GetOrAddNew(hub);
		NetworkReaderPooled reader = NetworkReaderPool.Get(msg.Payload);
		foreach (ServerSpecificSettingBase item in orAddNew)
		{
			if (item.SettingId == msg.Id && !(item.GetType() != msg.SettingType))
			{
				ServerSpecificSettingsSync.ServerDeserializeClientResponse(hub, item, reader);
				return;
			}
		}
		ServerSpecificSettingBase serverSpecificSettingBase = ServerSpecificSettingsSync.CreateInstance(msg.SettingType);
		orAddNew.Add(serverSpecificSettingBase);
		serverSpecificSettingBase.SetId(msg.Id, null);
		serverSpecificSettingBase.ApplyDefaultValues();
		ServerSpecificSettingsSync.ServerDeserializeClientResponse(hub, serverSpecificSettingBase, reader);
	}

	private static void ServerProcessClientStatusMsg(NetworkConnection conn, SSSUserStatusReport msg)
	{
		if (ReferenceHub.TryGetHub(conn, out var hub))
		{
			ServerSpecificSettingsSync.ReceivedUserStatuses[hub] = msg;
			ServerSpecificSettingsSync.ServerOnStatusReceived?.Invoke(hub, msg);
		}
	}
}
