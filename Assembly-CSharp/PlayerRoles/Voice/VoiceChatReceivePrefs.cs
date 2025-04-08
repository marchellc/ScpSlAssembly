using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UserSettings;

namespace PlayerRoles.Voice
{
	public class VoiceChatReceivePrefs
	{
		public static event Action<NetworkConnection, GroupMuteFlags> OnFlagsReceived;

		private static void ClientSendMessage()
		{
			GroupMuteFlags groupMuteFlags = GroupMuteFlags.None;
			foreach (GroupMuteFlags groupMuteFlags2 in EnumUtils<GroupMuteFlags>.Values)
			{
				if (UserSetting<bool>.Get<GroupMuteFlags>(groupMuteFlags2))
				{
					groupMuteFlags |= groupMuteFlags2;
				}
			}
			NetworkClient.Send<VoiceChatReceivePrefs.GroupMuteFlagsMessage>(new VoiceChatReceivePrefs.GroupMuteFlagsMessage
			{
				Flags = (byte)groupMuteFlags
			}, 0);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				VoiceChatReceivePrefs.RememberedFlags.Clear();
				NetworkServer.ReplaceHandler<VoiceChatReceivePrefs.GroupMuteFlagsMessage>(new Action<NetworkConnectionToClient, VoiceChatReceivePrefs.GroupMuteFlagsMessage>(VoiceChatReceivePrefs.ProcessMessage), true);
				VoiceChatReceivePrefs.ClientSendMessage();
			};
			GroupMuteFlags[] values = EnumUtils<GroupMuteFlags>.Values;
			for (int i = 0; i < values.Length; i++)
			{
				UserSetting<bool>.AddListener<GroupMuteFlags>(values[i], delegate(bool _)
				{
					VoiceChatReceivePrefs.ClientSendMessage();
				});
			}
		}

		private static void ProcessMessage(NetworkConnection conn, VoiceChatReceivePrefs.GroupMuteFlagsMessage msg)
		{
			GroupMuteFlags flags = (GroupMuteFlags)msg.Flags;
			Action<NetworkConnection, GroupMuteFlags> onFlagsReceived = VoiceChatReceivePrefs.OnFlagsReceived;
			if (onFlagsReceived != null)
			{
				onFlagsReceived(conn, flags);
			}
			VoiceChatReceivePrefs.RememberedFlags[conn] = flags;
			if (conn.identity == null)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			IVoiceRole voiceRole = referenceHub.roleManager.CurrentRole as IVoiceRole;
			if (voiceRole == null)
			{
				return;
			}
			voiceRole.VoiceModule.ReceiveFlags = flags;
		}

		public static GroupMuteFlags GetFlagsForUser(ReferenceHub hub)
		{
			GroupMuteFlags groupMuteFlags;
			if (!VoiceChatReceivePrefs.RememberedFlags.TryGetValue(hub.connectionToClient, out groupMuteFlags))
			{
				groupMuteFlags = GroupMuteFlags.None;
			}
			return groupMuteFlags;
		}

		private static readonly Dictionary<NetworkConnection, GroupMuteFlags> RememberedFlags = new Dictionary<NetworkConnection, GroupMuteFlags>();

		public struct GroupMuteFlagsMessage : NetworkMessage
		{
			public byte Flags;
		}
	}
}
