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
		NetworkClient.Send(new GroupMuteFlagsMessage
		{
			Flags = (byte)groupMuteFlags
		});
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			VoiceChatReceivePrefs.RememberedFlags.Clear();
			NetworkServer.ReplaceHandler<GroupMuteFlagsMessage>(ProcessMessage);
			VoiceChatReceivePrefs.ClientSendMessage();
		};
		GroupMuteFlags[] values = EnumUtils<GroupMuteFlags>.Values;
		for (int num = 0; num < values.Length; num++)
		{
			UserSetting<bool>.AddListener(values[num], delegate
			{
				VoiceChatReceivePrefs.ClientSendMessage();
			});
		}
	}

	private static void ProcessMessage(NetworkConnection conn, GroupMuteFlagsMessage msg)
	{
		GroupMuteFlags flags = (GroupMuteFlags)msg.Flags;
		VoiceChatReceivePrefs.OnFlagsReceived?.Invoke(conn, flags);
		VoiceChatReceivePrefs.RememberedFlags[conn] = flags;
		if (!(conn.identity == null) && ReferenceHub.TryGetHubNetID(conn.identity.netId, out var hub) && hub.roleManager.CurrentRole is IVoiceRole voiceRole)
		{
			voiceRole.VoiceModule.ReceiveFlags = flags;
		}
	}

	public static GroupMuteFlags GetFlagsForUser(ReferenceHub hub)
	{
		if (!VoiceChatReceivePrefs.RememberedFlags.TryGetValue(hub.connectionToClient, out var value))
		{
			return GroupMuteFlags.None;
		}
		return value;
	}
}
