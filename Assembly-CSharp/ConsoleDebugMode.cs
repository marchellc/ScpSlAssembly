using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ConsoleDebugMode
{
	public struct DebugChannel
	{
		public DebugLevel Level;

		public string Description;

		public Color32 Color;

		public DebugChannel(DebugLevel lvl, string dsc, Color32 col)
		{
			this.Level = lvl;
			this.Description = dsc;
			this.Color = col;
		}
	}

	public static readonly Color32 defaultColor = new Color32(85, 181, 125, byte.MaxValue);

	private static readonly Dictionary<string, DebugChannel> debugChannels = new Dictionary<string, DebugChannel>
	{
		["SCP079"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP079", 2), 0, 4), "for SCP-079 client- and server-side logging", ConsoleDebugMode.defaultColor),
		["MAPGEN"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MAPGEN", 2), 0, 4), "for the Map Generator", ConsoleDebugMode.defaultColor),
		["MISCEL"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MISCEL", 2), 0, 4), "for miscellaneous small sub-systems", ConsoleDebugMode.defaultColor),
		["VC"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_VC", 2), 0, 4), "for Voice Chat logging", ConsoleDebugMode.defaultColor),
		["PLIST"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_PLIST", 2), 0, 4), "for Player List", ConsoleDebugMode.defaultColor),
		["MGCLTR"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_MGCLTR", 2), 0, 4), "for Map Generator Clutter System", new Color32(110, 160, 110, byte.MaxValue)),
		["SDAUTH"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SDAUTH", 2), 0, 4), "for Steam and Discord authenticator", new Color32(130, 130, 130, byte.MaxValue)),
		["SCPCTRL"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCPCTRL", 2), 0, 4), "for playeable SCP controller", new Color32(130, 130, 130, byte.MaxValue)),
		["SCP096"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP096", 2), 0, 4), "for SCP-096 client and server-side logging", ConsoleDebugMode.defaultColor),
		["SCP173"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SCP173", 2), 0, 4), "for SCP-173 client and server-side logging", ConsoleDebugMode.defaultColor),
		["SEARCH"] = new DebugChannel((DebugLevel)Mathf.Clamp(PlayerPrefsSl.Get("DEBUG_SEARCH", 2), 0, 4), "for Search System logging", ConsoleDebugMode.defaultColor)
	};

	public static bool CheckImportance(string key, MessageImportance importance)
	{
		if (ConsoleDebugMode.debugChannels.TryGetValue(key, out var value))
		{
			return (int)value.Level >= (int)importance;
		}
		return true;
	}

	public static bool CheckImportance(string key, MessageImportance importance, out Color32 color)
	{
		if (ConsoleDebugMode.debugChannels.TryGetValue(key, out var value))
		{
			color = value.Color;
			return (int)value.Level >= (int)importance;
		}
		color = ConsoleDebugMode.defaultColor;
		return true;
	}

	public static void GetList(out string[] keys, out string[] descriptions)
	{
		keys = ConsoleDebugMode.debugChannels.Keys.ToArray();
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
		ConsoleDebugMode.debugChannels[key] = new DebugChannel((DebugLevel)newLevel, ConsoleDebugMode.debugChannels[key].Description, ConsoleDebugMode.debugChannels[key].Color);
		PlayerPrefsSl.Set("DEBUG_" + key, newLevel);
		return true;
	}

	public static string ConsoleGetLevel(string key)
	{
		key = key.ToUpper();
		if (ConsoleDebugMode.debugChannels.TryGetValue(key, out var value))
		{
			return "The " + key + " is currently on the <i>" + value.Level.ToString().ToLower() + "</i> debug level.";
		}
		return "Module '" + key + "' could not be found.";
	}
}
