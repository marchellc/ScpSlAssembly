using System;
using System.Collections.Generic;
using System.IO;
using CentralAuth;
using GameCore;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace VoiceChat
{
	public static class VoiceChatMutes
	{
		public static event Action<ReferenceHub, VcMuteFlags> OnFlagsSet;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(VoiceChatMutes.LoadMutes));
			CustomNetworkManager.OnClientReady += delegate
			{
				if (VoiceChatMutes._everLoaded)
				{
					return;
				}
				VoiceChatMutes.LoadMutes();
			};
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				if (VoiceChatMutes.GetFlags(hub) != VcMuteFlags.None)
				{
					Action<ReferenceHub, VcMuteFlags> onFlagsSet = VoiceChatMutes.OnFlagsSet;
					if (onFlagsSet != null)
					{
						onFlagsSet(hub, VcMuteFlags.None);
					}
				}
				VoiceChatMutes.Flags.Remove(hub);
			}));
			PlayerAuthenticationManager.OnSyncedUserIdAssigned += delegate(ReferenceHub hub)
			{
				if (NetworkServer.active)
				{
					return;
				}
				if (!VoiceChatMutes.QueryLocalMute(hub.authManager.SyncedUserId, false))
				{
					return;
				}
				VoiceChatMutes.SetFlags(hub, VcMuteFlags.LocalRegular);
			};
			VoiceChatMutes.LoadMutes();
		}

		private static void LoadMutes()
		{
			VoiceChatMutes._path = ConfigSharing.Paths[1];
			if (string.IsNullOrEmpty(VoiceChatMutes._path))
			{
				return;
			}
			VoiceChatMutes._path += "mutes.txt";
			VoiceChatMutes._everLoaded = true;
			try
			{
				using (StreamReader streamReader = new StreamReader(VoiceChatMutes._path))
				{
					for (;;)
					{
						string text = streamReader.ReadLine();
						if (text == null)
						{
							break;
						}
						string text2;
						if (VoiceChatMutes.TryValidateId(text, false, out text2))
						{
							VoiceChatMutes.Mutes.Add(text2);
						}
					}
				}
			}
			catch
			{
				global::GameCore.Console.AddLog("Can't load the mute file!", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
			}
		}

		private static bool TryValidateId(string raw, bool intercom, out string validated)
		{
			validated = ((raw != null) ? raw.Trim() : null);
			if (string.IsNullOrEmpty(raw))
			{
				return false;
			}
			if (intercom)
			{
				validated = "ICOM-" + validated;
			}
			return true;
		}

		private static bool TryGetHub(string userId, out ReferenceHub hub)
		{
			return ReferenceHub.AllHubs.TryGetFirst((ReferenceHub x) => VoiceChatMutes.CheckHub(x, userId), out hub);
		}

		private static bool CheckHub(ReferenceHub hub, string id)
		{
			PlayerAuthenticationManager authManager = hub.authManager;
			return authManager.UserId == id || (NetworkServer.active && authManager.SyncedUserId == id);
		}

		private static VcMuteFlags GetLocalFlag(bool intercom)
		{
			if (!intercom)
			{
				return VcMuteFlags.LocalRegular;
			}
			return VcMuteFlags.LocalIntercom;
		}

		public static bool QueryLocalMute(string userId, bool intercom = false)
		{
			string text;
			return VoiceChatMutes.TryValidateId(userId, intercom, out text) && VoiceChatMutes.Mutes.Contains(text);
		}

		public static void IssueLocalMute(string userId, bool intercom = false)
		{
			string text;
			if (!VoiceChatMutes.TryValidateId(userId, intercom, out text))
			{
				return;
			}
			if (!VoiceChatMutes.Mutes.Add(text))
			{
				return;
			}
			File.AppendAllText(VoiceChatMutes._path, "\r\n" + text);
			ReferenceHub referenceHub;
			if (!VoiceChatMutes.TryGetHub(userId, out referenceHub))
			{
				return;
			}
			VoiceChatMutes.SetFlags(referenceHub, VoiceChatMutes.GetFlags(referenceHub) | VoiceChatMutes.GetLocalFlag(intercom));
		}

		public static void RevokeLocalMute(string userId, bool intercom = false)
		{
			string text;
			if (!VoiceChatMutes.TryValidateId(userId, intercom, out text))
			{
				return;
			}
			if (!VoiceChatMutes.Mutes.Remove(text))
			{
				return;
			}
			FileManager.WriteToFile(VoiceChatMutes.Mutes, VoiceChatMutes._path, false);
			ReferenceHub referenceHub;
			if (!VoiceChatMutes.TryGetHub(userId, out referenceHub))
			{
				return;
			}
			VoiceChatMutes.SetFlags(referenceHub, VoiceChatMutes.GetFlags(referenceHub) & ~VoiceChatMutes.GetLocalFlag(intercom));
		}

		public static void SetFlags(ReferenceHub hub, VcMuteFlags flags)
		{
			VoiceChatMutes.Flags[hub] = flags;
			Action<ReferenceHub, VcMuteFlags> onFlagsSet = VoiceChatMutes.OnFlagsSet;
			if (onFlagsSet == null)
			{
				return;
			}
			onFlagsSet(hub, flags);
		}

		public static VcMuteFlags GetFlags(ReferenceHub hub)
		{
			VcMuteFlags vcMuteFlags;
			if (!VoiceChatMutes.Flags.TryGetValue(hub, out vcMuteFlags))
			{
				return VcMuteFlags.None;
			}
			return vcMuteFlags;
		}

		public static bool IsMuted(ReferenceHub hub, bool checkIntercom = false)
		{
			VcMuteFlags flags = VoiceChatMutes.GetFlags(hub);
			if (checkIntercom)
			{
				return flags > VcMuteFlags.None;
			}
			return (flags & (VcMuteFlags.LocalRegular | VcMuteFlags.GlobalRegular)) > VcMuteFlags.None;
		}

		private const string Filename = "mutes.txt";

		private const string IntercomPrefix = "ICOM-";

		private static string _path;

		private static bool _everLoaded;

		private static readonly HashSet<string> Mutes = new HashSet<string>();

		private static readonly Dictionary<ReferenceHub, VcMuteFlags> Flags = new Dictionary<ReferenceHub, VcMuteFlags>();
	}
}
