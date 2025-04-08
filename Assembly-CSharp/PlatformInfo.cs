using System;
using System.Runtime.InteropServices;
using System.Threading;
using GameCore;
using NorthwoodLib;
using NorthwoodLib.Logging;
using UnityEngine;

public class PlatformInfo : MonoBehaviour
{
	public bool IsHeadless { get; } = true;

	public bool IsIl2Cpp { get; }

	public bool IsEditor { get; }

	public bool UsesCustomLauncher { get; }

	public string BuildGuid { get; private set; }

	public bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	public bool IsLinux { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

	public int MainThreadId { get; private set; }

	public bool IsMainThread
	{
		get
		{
			return Thread.CurrentThread.ManagedThreadId == this.MainThreadId;
		}
	}

	private void Awake()
	{
		this.MainThreadId = Thread.CurrentThread.ManagedThreadId;
		this.BuildGuid = Application.buildGUID;
		PlatformInfo.singleton = this;
		global::GameCore.Console.AddLog("Loaded NorthwoodLib " + PlatformSettings.Version, Color.green, false, global::GameCore.Console.ConsoleLogType.Log);
		PlatformSettings.Logged += PlatformInfo.OnLogged;
	}

	private static void OnLogged(string text, global::NorthwoodLib.Logging.LogType type)
	{
		switch (type)
		{
		case global::NorthwoodLib.Logging.LogType.Debug:
			Debug.Log(text);
			return;
		case global::NorthwoodLib.Logging.LogType.Info:
			global::GameCore.Console.AddLog(text, Color.blue, false, global::GameCore.Console.ConsoleLogType.Log);
			return;
		case global::NorthwoodLib.Logging.LogType.Warning:
			global::GameCore.Console.AddLog(text, Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
			return;
		case global::NorthwoodLib.Logging.LogType.Error:
			global::GameCore.Console.AddLog(text, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			return;
		default:
			Debug.Log(text);
			return;
		}
	}

	public static PlatformInfo singleton;
}
