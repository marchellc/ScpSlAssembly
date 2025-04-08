using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ConsoleDebugMode
{
	public static bool CheckImportance(string key, MessageImportance importance)
	{
		ConsoleDebugMode.DebugChannel debugChannel;
		return !ConsoleDebugMode.debugChannels.TryGetValue(key, out debugChannel) || debugChannel.Level >= (DebugLevel)importance;
	}

	public static bool CheckImportance(string key, MessageImportance importance, out Color32 color)
	{
		ConsoleDebugMode.DebugChannel debugChannel;
		if (ConsoleDebugMode.debugChannels.TryGetValue(key, out debugChannel))
		{
			color = debugChannel.Color;
			return debugChannel.Level >= (DebugLevel)importance;
		}
		color = ConsoleDebugMode.defaultColor;
		return true;
	}

	public static void GetList(out string[] keys, out string[] descriptions)
	{
		keys = ConsoleDebugMode.debugChannels.Keys.ToArray<string>();
		descriptions = new string[ConsoleDebugMode.debugChannels.Keys.Count];
		for (int i = 0; i < descriptions.Length; i++)
		{
			descriptions[i] = ConsoleDebugMode.debugChannels[keys[i]].Description;
		}
	}

	public static bool ChangeImportance(string key, int newLevel)
	{
		if (!ConsoleDebugMode.debugChannels.ContainsKey(key))
		{
			return false;
		}
		ConsoleDebugMode.debugChannels[key] = new ConsoleDebugMode.DebugChannel((DebugLevel)newLevel, ConsoleDebugMode.debugChannels[key].Description, ConsoleDebugMode.debugChannels[key].Color);
		PlayerPrefsSl.Set("DEBUG_" + key, newLevel);
		return true;
	}

	public static string ConsoleGetLevel(string key)
	{
		key = key.ToUpper();
		ConsoleDebugMode.DebugChannel debugChannel;
		if (ConsoleDebugMode.debugChannels.TryGetValue(key, out debugChannel))
		{
			return string.Concat(new string[]
			{
				"The ",
				key,
				" is currently on the <i>",
				debugChannel.Level.ToString().ToLower(),
				"</i> debug level."
			});
		}
		return "Module '" + key + "' could not be found.";
	}

	// Note: this type is marked as 'beforefieldinit'.
	static ConsoleDebugMode()
	{
		Dictionary<string, ConsoleDebugMode.DebugChannel> dictionary = new Dictionary<string, ConsoleDebugMode.DebugChannel>();
		dictionary["SCP079"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP079", 2), 0, 4), "for SCP-079 client- and server-side logging", ConsoleDebugMode.defaultColor);
		dictionary["MAPGEN"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MAPGEN", 2), 0, 4), "for the Map Generator", ConsoleDebugMode.defaultColor);
		dictionary["MISCEL"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MISCEL", 2), 0, 4), "for miscellaneous small sub-systems", ConsoleDebugMode.defaultColor);
		dictionary["VC"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_VC", 2), 0, 4), "for Voice Chat logging", ConsoleDebugMode.defaultColor);
		dictionary["PLIST"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_PLIST", 2), 0, 4), "for Player List", ConsoleDebugMode.defaultColor);
		dictionary["MGCLTR"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MGCLTR", 2), 0, 4), "for Map Generator Clutter System", new Color32(110, 160, 110, byte.MaxValue));
		dictionary["SDAUTH"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SDAUTH", 2), 0, 4), "for Steam and Discord authenticator", new Color32(130, 130, 130, byte.MaxValue));
		dictionary["SCPCTRL"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCPCTRL", 2), 0, 4), "for playeable SCP controller", new Color32(130, 130, 130, byte.MaxValue));
		dictionary["SCP096"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP096", 2), 0, 4), "for SCP-096 client and server-side logging", ConsoleDebugMode.defaultColor);
		dictionary["SCP173"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP173", 2), 0, 4), "for SCP-173 client and server-side logging", ConsoleDebugMode.defaultColor);
		dictionary["SEARCH"] = new ConsoleDebugMode.DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SEARCH", 2), 0, 4), "for Search System logging", ConsoleDebugMode.defaultColor);
		ConsoleDebugMode.debugChannels = dictionary;
	}

	public static readonly Color32 defaultColor = new Color32(85, 181, 125, byte.MaxValue);

	private static readonly Dictionary<string, ConsoleDebugMode.DebugChannel> debugChannels;

	public struct DebugChannel
	{
		public DebugChannel(DebugLevel lvl, string dsc, Color32 col)
		{
			this.Level = lvl;
			this.Description = dsc;
			this.Color = col;
		}

		public DebugLevel Level;

		public string Description;

		public Color32 Color;
	}
}
