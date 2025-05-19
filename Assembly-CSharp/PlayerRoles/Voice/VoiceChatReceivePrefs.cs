using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UserSettings;

namespace PlayerRoles.Voice;

public class VoiceChatReceivePrefs
{
	public struct GroupMuteFlagsMessage : NetworkMessage
	{
		public byte Flags;
	}

	private static readonly Dictionary<NetworkConnection, GroupMuteFlags> RememberedFlags = new Dictionary<NetworkConnection, GroupMuteFlags>();

	public static event Action<NetworkConnection, GroupMuteFlags> OnFlagsReceived;

	private static void ClientSendMessage()
	{
		GroupMuteFlags groupMuteFlags = GroupMuteFlags.None;
		GroupMuteFlags[] values = EnumUtils<GroupMuteFlags>.Values;
		foreach (GroupMuteFlags groupMuteFlags2 in values)
		{
			if (UserSetting<bool>.Get(groupMuteFlags2))
			{
				groupMuteFlags |= groupMuteFlags2;
			}
		}
		GroupMuteFlagsMessage message = default(GroupMuteFlagsMessage);
		message.Flags = (byte)groupMuteFlags;
		NetworkClient.Send(message);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			RememberedFlags.Clear();
			NetworkServer.ReplaceHandler<GroupMuteFlagsMessage>(ProcessMessage);
			ClientSendMessage();
		};
		GroupMuteFlags[] values = EnumUtils<GroupMuteFlags>.Values;
		for (int i = 0; i < values.Length; i++)
		{
			UserSetting<bool>.AddListener(values[i], delegate
			{
				ClientSendMessage();
			});
		}
	}

	private static void ProcessMessage(NetworkConnection conn, GroupMuteFlagsMessage msg)
	{
		GroupMuteFlags flags = (GroupMuteFlags)msg.Flags;
		VoiceChatReceivePrefs.OnFlagsReceived?.Invoke(conn, flags);
		RememberedFlags[conn] = flags;
		if (!(conn.identity == null) && ReferenceHub.TryGetHubNetID(conn.identity.netId, out var hub) && hub.roleManager.CurrentRole is IVoiceRole voiceRole)
		{
			voiceRole.VoiceModule.ReceiveFlags = flags;
		}
	}

	public static GroupMuteFlags GetFlagsForUser(ReferenceHub hub)
	{
		if (!RememberedFlags.TryGetValue(hub.connectionToClient, out var value))
		{
			return GroupMuteFlags.None;
		}
		return value;
	}
}
