using System;
using System.Diagnostics;
using LabApi.Loader;
using ServerOutput;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerStatic : MonoBehaviour
{
	public static bool IsDedicated { get; private set; }

	public static ServerStatic.NextRoundAction StopNextRound { get; set; } = ServerStatic.NextRoundAction.DoNothing;

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
			if (!ServerStatic.IsDedicated)
			{
				return;
			}
			Application.targetFrameRate = (int)ServerStatic._serverTickrate;
			ServerConsole.AddLog("Server tickrate set to: " + ServerStatic._serverTickrate.ToString(), ConsoleColor.Gray, false);
		}
	}

	public static ushort ServerPort { get; private set; }

	private void Awake()
	{
		ServerStatic.ProcessServerArgs();
		if (!ServerStatic.IsDedicated)
		{
			Shutdown.Quit(true, false);
			return;
		}
		if (ServerStatic.ServerOutput == null)
		{
			Shutdown.Quit(true, false);
			return;
		}
		ServerStatic.ServerOutput.Start();
		if (ServerStatic.IsDedicated)
		{
			AudioListener.volume = 0f;
			AudioListener.pause = true;
			QualitySettings.pixelLightCount = 0;
			GUI.enabled = false;
			ServerConsole.AddLog("SCP Secret Laboratory process started. Creating match...", ConsoleColor.Green, false);
			ServerStatic.ServerTickrate = 60;
			if (!ServerStatic._serverPortSet)
			{
				ServerConsole.AddLog("\"-port\" argument is required for dedicated server. Aborting startup.", ConsoleColor.DarkRed, false);
				ServerConsole.AddLog("Make sure you are using latest version of LocalAdmin.", ConsoleColor.DarkRed, false);
				Shutdown.Quit(true, false);
				return;
			}
		}
		PluginLoader.Initialize();
		SceneManager.sceneLoaded += this.OnSceneWasLoaded;
	}

	[RuntimeInitializeOnLoadMethod]
	internal static void ProcessServerArgs()
	{
		if (ServerStatic._serverArgsProcessed)
		{
			return;
		}
		ServerStatic._serverArgsProcessed = true;
		int num = 0;
		int num2 = 0;
		int i = 0;
		while (i < StartupArgs.Args.Length)
		{
			string text = StartupArgs.Args[i];
			string text2 = text;
			uint num3 = <PrivateImplementationDetails>.ComputeStringHash(text2);
			if (num3 <= 944841373U)
			{
				if (num3 <= 412146683U)
				{
					if (num3 != 330659931U)
					{
						if (num3 != 412146683U)
						{
							goto IL_0258;
						}
						if (!(text2 == "-stdout"))
						{
							goto IL_0258;
						}
						if (ServerStatic._serverPortSet || ServerStatic.ServerOutput != null)
						{
							goto IL_0258;
						}
						ServerStatic.ServerOutput = new StandardOutput();
					}
					else
					{
						if (!(text2 == "-keepsession"))
						{
							goto IL_0258;
						}
						ServerStatic.KeepSession = true;
					}
				}
				else if (num3 != 533076168U)
				{
					if (num3 != 944841373U)
					{
						goto IL_0258;
					}
					if (!(text2 == "-configpath"))
					{
						goto IL_0258;
					}
					if (i >= StartupArgs.Args.Length - 1)
					{
						goto IL_0258;
					}
					FileManager.SetConfigFolder(StartupArgs.Args[i + 1]);
				}
				else
				{
					if (!(text2 == "-heartbeat"))
					{
						goto IL_0258;
					}
					ServerStatic.EnableConsoleHeartbeat = true;
				}
			}
			else if (num3 <= 1273769260U)
			{
				if (num3 != 972287825U)
				{
					if (num3 != 1273769260U)
					{
						goto IL_0258;
					}
					if (!(text2 == "-appdatapath"))
					{
						goto IL_0258;
					}
					if (i >= StartupArgs.Args.Length - 1)
					{
						goto IL_0258;
					}
					FileManager.SetAppFolder(StartupArgs.Args[i + 1]);
				}
				else
				{
					if (!(text2 == "-disableconfigvalidation"))
					{
						goto IL_0258;
					}
					ServerStatic.DisableConfigValidation = true;
				}
			}
			else if (num3 != 1486336508U)
			{
				if (num3 != 2759251362U)
				{
					if (num3 != 3057086030U)
					{
						goto IL_0258;
					}
					if (!(text2 == "-txbuffer"))
					{
						goto IL_0258;
					}
					if (i >= StartupArgs.Args.Length - 1)
					{
						goto IL_0258;
					}
					if (!int.TryParse(StartupArgs.Args[i + 1], out num))
					{
						goto IL_0258;
					}
				}
				else
				{
					if (!(text2 == "-nographics"))
					{
						goto IL_0258;
					}
					ServerStatic.IsDedicated = true;
				}
			}
			else
			{
				if (!(text2 == "-rxbuffer"))
				{
					goto IL_0258;
				}
				if (i >= StartupArgs.Args.Length - 1)
				{
					goto IL_0258;
				}
				if (!int.TryParse(StartupArgs.Args[i + 1], out num2))
				{
					goto IL_0258;
				}
			}
			IL_0380:
			i++;
			continue;
			IL_0258:
			if (text.StartsWith("-key", StringComparison.Ordinal) && text.Length > 4 && !ServerStatic._serverPortSet && ServerStatic.ServerOutput == null)
			{
				ServerStatic.ServerOutput = new FileConsole(text.Remove(0, 4));
				goto IL_0380;
			}
			if (text.StartsWith("-console", StringComparison.Ordinal) && ServerStatic.ServerOutput == null)
			{
				ushort num4;
				if (ushort.TryParse(text.Remove(0, 8), out num4))
				{
					ServerStatic.ServerOutput = new TcpConsole(num4, num2, num);
					goto IL_0380;
				}
				goto IL_0380;
			}
			else
			{
				if (text.StartsWith("-id", StringComparison.Ordinal) && !ServerStatic.ProcessIdPassed)
				{
					ServerStatic.ProcessIdPassed = true;
					int num5;
					if (int.TryParse(text.Remove(0, 3), out num5))
					{
						ServerConsole.ConsoleProcess = Process.GetProcessById(num5);
					}
					if (ServerConsole.ConsoleProcess == null)
					{
						ServerStatic.OnConsoleExited(null, null);
					}
					ServerConsole.ConsoleProcess.EnableRaisingEvents = true;
					ServerConsole.ConsoleProcess.Exited += ServerStatic.OnConsoleExited;
					goto IL_0380;
				}
				if (!text.StartsWith("-port", StringComparison.Ordinal) || ServerStatic._serverPortSet)
				{
					goto IL_0380;
				}
				ushort num6;
				if (!ushort.TryParse(text.Remove(0, 5), out num6))
				{
					ServerConsole.AddLog("\"-port\" argument value is not a valid unsigned short integer (0 - 65535). Aborting startup.", ConsoleColor.Gray, false);
					Shutdown.Quit(true, false);
					return;
				}
				ServerStatic.ServerPort = num6;
				ServerStatic._serverPortSet = true;
				goto IL_0380;
			}
		}
	}

	private static void OnConsoleExited(object sender, EventArgs e)
	{
		ServerConsole.DisposeStatic();
		ServerStatic.IsDedicated = false;
		global::UnityEngine.Debug.Log("OnConsoleExited");
		Process consoleProcess = ServerConsole.ConsoleProcess;
		if (consoleProcess != null)
		{
			consoleProcess.Dispose();
		}
		ServerConsole.ConsoleProcess = null;
		Shutdown.Quit(true, false);
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

	public enum NextRoundAction : byte
	{
		DoNothing,
		Restart,
		Shutdown
	}
}
