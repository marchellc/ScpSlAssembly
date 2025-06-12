using System;
using System.Collections.Generic;
using System.IO;
using CentralAuth;
using GameCore;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace VoiceChat;

public static class VoiceChatMutes
{
	private const string Filename = "mutes.txt";

	private const string IntercomPrefix = "ICOM-";

	private static string _path;

	private static bool _everLoaded;

	private static readonly HashSet<string> Mutes = new HashSet<string>();

	private static readonly Dictionary<ReferenceHub, VcMuteFlags> Flags = new Dictionary<ReferenceHub, VcMuteFlags>();

	public static event Action<ReferenceHub, VcMuteFlags> OnFlagsSet;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(LoadMutes));
		CustomNetworkManager.OnClientReady += delegate
		{
			if (!VoiceChatMutes._everLoaded)
			{
				VoiceChatMutes.LoadMutes();
			}
		};
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			if (VoiceChatMutes.GetFlags(hub) != VcMuteFlags.None)
			{
				VoiceChatMutes.OnFlagsSet?.Invoke(hub, VcMuteFlags.None);
			}
			VoiceChatMutes.Flags.Remove(hub);
		};
		PlayerAuthenticationManager.OnSyncedUserIdAssigned += delegate(ReferenceHub hub)
		{
			if (!NetworkServer.active && VoiceChatMutes.QueryLocalMute(hub.authManager.SyncedUserId))
			{
				VoiceChatMutes.SetFlags(hub, VcMuteFlags.LocalRegular);
			}
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
			using StreamReader streamReader = new StreamReader(VoiceChatMutes._path);
			while (true)
			{
				string text = streamReader.ReadLine();
				if (text != null)
				{
					if (VoiceChatMutes.TryValidateId(text, intercom: false, out var validated))
					{
						VoiceChatMutes.Mutes.Add(validated);
					}
					continue;
				}
				break;
			}
		}
		catch
		{
			GameCore.Console.AddLog("Can't load the mute file!", Color.yellow);
		}
	}

	private static bool TryValidateId(string raw, bool intercom, out string validated)
	{
		validated = raw?.Trim();
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
		if (!(authManager.UserId == id))
		{
			if (NetworkServer.active)
			{
				return authManager.SyncedUserId == id;
			}
			return false;
		}
		return true;
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
		if (VoiceChatMutes.TryValidateId(userId, intercom, out var validated))
		{
			return VoiceChatMutes.Mutes.Contains(validated);
		}
		return false;
	}

	public static void IssueLocalMute(string userId, bool intercom = false)
	{
		if (VoiceChatMutes.TryValidateId(userId, intercom, out var validated) && VoiceChatMutes.Mutes.Add(validated))
		{
			File.AppendAllText(VoiceChatMutes._path, "\r\n" + validated);
			if (VoiceChatMutes.TryGetHub(userId, out var hub))
			{
				VoiceChatMutes.SetFlags(hub, VoiceChatMutes.GetFlags(hub) | VoiceChatMutes.GetLocalFlag(intercom));
			}
		}
	}

	public static void RevokeLocalMute(string userId, bool intercom = false)
	{
		if (VoiceChatMutes.TryValidateId(userId, intercom, out var validated) && VoiceChatMutes.Mutes.Remove(validated))
		{
			FileManager.WriteToFile(VoiceChatMutes.Mutes, VoiceChatMutes._path);
			if (VoiceChatMutes.TryGetHub(userId, out var hub))
			{
				VoiceChatMutes.SetFlags(hub, (VcMuteFlags)((uint)VoiceChatMutes.GetFlags(hub) & (uint)(byte)(~(int)VoiceChatMutes.GetLocalFlag(intercom))));
			}
		}
	}

	public static void SetFlags(ReferenceHub hub, VcMuteFlags flags)
	{
		VoiceChatMutes.Flags[hub] = flags;
		VoiceChatMutes.OnFlagsSet?.Invoke(hub, flags);
	}

	public static VcMuteFlags GetFlags(ReferenceHub hub)
	{
		if (!VoiceChatMutes.Flags.TryGetValue(hub, out var value))
		{
			return VcMuteFlags.None;
		}
		return value;
	}

	public static bool IsMuted(ReferenceHub hub, bool checkIntercom = false)
	{
		VcMuteFlags flags = VoiceChatMutes.GetFlags(hub);
		if (checkIntercom)
		{
			return flags != VcMuteFlags.None;
		}
		return (flags & (VcMuteFlags.LocalRegular | VcMuteFlags.GlobalRegular)) != 0;
	}
}
