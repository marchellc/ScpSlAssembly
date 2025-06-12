using System;
using System.Diagnostics;
using LabApi.Loader;
using ServerOutput;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerStatic : MonoBehaviour
{
	public enum NextRoundAction : byte
	{
		DoNothing,
		Restart,
		Shutdown
	}

	public static ushort ShutdownRedirectPort;

	public static YamlConfig RolesConfig;

	public static YamlConfig SharedGroupsConfig;

	public static YamlConfig SharedGroupsMembersConfig;

	public static string RolesConfigPath;

	public static PermissionsHandler PermissionsHandler;

	public static IServerOutput ServerOutput;

	internal static bool ProcessIdPassed;

	internal static bool DisableConfigValidation;

	internal static bool KeepSession;

	internal static bool EnableConsoleHeartbeat;

	private static short _serverTickrate = 60;

	private static bool _serverArgsProcessed;

	private static bool _serverPortSet;

	public static bool IsDedicated { get; private set; }

	public static NextRoundAction StopNextRound { get; set; } = NextRoundAction.DoNothing;

	public static short ServerTickrate
	{
		get
		{
			return ServerStatic._serverTickrate;
		}
		set
		{
			if (value < -1 || value == 0)
			{
				ServerStatic._serverTickrate = 60;
			}
			else
			{
				ServerStatic._serverTickrate = value;
			}
			if (ServerStatic.IsDedicated)
			{
				Application.targetFrameRate = ServerStatic._serverTickrate;
				ServerConsole.AddLog("Server tickrate set to: " + ServerStatic._serverTickrate);
			}
		}
	}

	public static ushort ServerPort { get; private set; }

	private void Awake()
	{
		ServerStatic.ProcessServerArgs();
		if (!ServerStatic.IsDedicated)
		{
			Shutdown.Quit();
			return;
		}
		if (ServerStatic.ServerOutput == null)
		{
			Shutdown.Quit();
			return;
		}
		ServerStatic.ServerOutput.Start();
		if (ServerStatic.IsDedicated)
		{
			AudioListener.volume = 0f;
			AudioListener.pause = true;
			QualitySettings.pixelLightCount = 0;
			ServerConsole.AddLog("SCP Secret Laboratory process started. Creating match...", ConsoleColor.Green);
			ServerStatic.ServerTickrate = 60;
			if (!ServerStatic._serverPortSet)
			{
				ServerConsole.AddLog("\"-port\" argument is required for dedicated server. Aborting startup.", ConsoleColor.DarkRed);
				ServerConsole.AddLog("Make sure you are using latest version of LocalAdmin.", ConsoleColor.DarkRed);
				Shutdown.Quit();
				return;
			}
		}
		PluginLoader.Initialize();
		SceneManager.sceneLoaded += OnSceneWasLoaded;
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void ProcessServerArgs()
	{
		if (ServerStatic._serverArgsProcessed)
		{
			return;
		}
		ServerStatic._serverArgsProcessed = true;
		int result = 0;
		int result2 = 0;
		for (int i = 0; i < StartupArgs.Args.Length; i++)
		{
			string text = StartupArgs.Args[i];
			switch (text)
			{
			case "-nographics":
				ServerStatic.IsDedicated = true;
				continue;
			case "-keepsession":
				ServerStatic.KeepSession = true;
				continue;
			case "-heartbeat":
				ServerStatic.EnableConsoleHeartbeat = true;
				continue;
			case "-appdatapath":
				if (i < StartupArgs.Args.Length - 1)
				{
					FileManager.SetAppFolder(StartupArgs.Args[i + 1]);
					continue;
				}
				break;
			case "-txbuffer":
				if (i < StartupArgs.Args.Length - 1 && int.TryParse(StartupArgs.Args[i + 1], out result))
				{
					continue;
				}
				break;
			case "-rxbuffer":
				if (i < StartupArgs.Args.Length - 1 && int.TryParse(StartupArgs.Args[i + 1], out result2))
				{
					continue;
				}
				break;
			case "-configpath":
				if (i < StartupArgs.Args.Length - 1)
				{
					FileManager.SetConfigFolder(StartupArgs.Args[i + 1]);
					continue;
				}
				break;
			case "-disableconfigvalidation":
				ServerStatic.DisableConfigValidation = true;
				continue;
			case "-stdout":
				if (!ServerStatic._serverPortSet && ServerStatic.ServerOutput == null)
				{
					ServerStatic.ServerOutput = new StandardOutput();
					continue;
				}
				break;
			}
			if (text.StartsWith("-key", StringComparison.Ordinal) && text.Length > 4 && !ServerStatic._serverPortSet && ServerStatic.ServerOutput == null)
			{
				ServerStatic.ServerOutput = new FileConsole(text.Remove(0, 4));
			}
			else if (text.StartsWith("-console", StringComparison.Ordinal) && ServerStatic.ServerOutput == null)
			{
				if (ushort.TryParse(text.Remove(0, 8), out var result3))
				{
					ServerStatic.ServerOutput = new TcpConsole(result3, result2, result);
				}
			}
			else if (text.StartsWith("-id", StringComparison.Ordinal) && !ServerStatic.ProcessIdPassed)
			{
				ServerStatic.ProcessIdPassed = true;
				if (int.TryParse(text.Remove(0, 3), out var result4))
				{
					ServerConsole.ConsoleProcess = Process.GetProcessById(result4);
				}
				if (ServerConsole.ConsoleProcess == null)
				{
					ServerStatic.OnConsoleExited(null, null);
				}
				ServerConsole.ConsoleProcess.EnableRaisingEvents = true;
				ServerConsole.ConsoleProcess.Exited += OnConsoleExited;
			}
			else if (text.StartsWith("-port", StringComparison.Ordinal) && !ServerStatic._serverPortSet)
			{
				if (!ushort.TryParse(text.Remove(0, 5), out var result5))
				{
					ServerConsole.AddLog("\"-port\" argument value is not a valid unsigned short integer (0 - 65535). Aborting startup.");
					Shutdown.Quit();
					break;
				}
				ServerStatic.ServerPort = result5;
				ServerStatic._serverPortSet = true;
			}
		}
	}

	private static void OnConsoleExited(object sender, EventArgs e)
	{
		ServerConsole.DisposeStatic();
		ServerStatic.IsDedicated = false;
		UnityEngine.Debug.Log("OnConsoleExited");
		ServerConsole.ConsoleProcess?.Dispose();
		ServerConsole.ConsoleProcess = null;
		Shutdown.Quit();
	}

	private void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
	{
		if (ServerStatic.IsDedicated)
		{
			int buildIndex = scene.buildIndex;
			if (buildIndex == 3 || buildIndex == 4)
			{
				base.GetComponent<CustomNetworkManager>().CreateMatch();
			}
		}
	}
}
