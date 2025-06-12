using System;
using System.Linq;
using CentralAuth;
using CommandSystem;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using Mirror;
using Org.BouncyCastle.Crypto;
using RemoteAdmin;
using ToggleableMenus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore;

public class Console : SimpleToggleableMenu
{
	public enum ConsoleLogType
	{
		DoNotLog,
		Log,
		Warning,
		Error
	}

	public CommandHint[] hints;

	public readonly GameConsoleCommandHandler ConsoleCommandHandler = GameConsoleCommandHandler.Create();

	internal static bool TranslationDebugMode;

	internal static AsymmetricKeyParameter _publicKey;

	private string _content;

	public static Console singleton { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		if (Console.singleton == null)
		{
			Console.singleton = this;
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(base.gameObject);
		}
	}

	private void Start()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		Console.AddLog("Hi there! Initializing console...", new Color32(0, byte.MaxValue, 0, byte.MaxValue));
		Console.AddLog("Done! Type 'help' to print the list of available commands.", new Color32(0, byte.MaxValue, 0, byte.MaxValue));
		CentralAuthManager.InitAuth();
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-tdm", StringComparison.OrdinalIgnoreCase)))
		{
			Console.TranslationDebugMode = true;
			Console.AddLog("Translation debug mode has been enabled (startup argument).", new Color32(0, byte.MaxValue, 0, byte.MaxValue));
		}
		else
		{
			Console.TranslationDebugMode = false;
		}
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Console.AddLog("Scene Manager: Loaded scene '" + scene.name + "' [" + scene.path + "]", new Color32(0, byte.MaxValue, 0, byte.MaxValue));
		this.RefreshConsoleScreen();
	}

	private void Update()
	{
	}

	private void LateUpdate()
	{
	}

	private void FixedUpdate()
	{
	}

	private void RefreshConsoleScreen()
	{
	}

	public static void AddDebugLog(string debugKey, string message, MessageImportance importance, bool nospace = false)
	{
		if (ConsoleDebugMode.CheckImportance(debugKey, importance, out var color))
		{
			Console.AddLog("[DEBUG_" + debugKey + "] " + message, color, nospace);
		}
	}

	public static void AddLog(string text, Color c, bool nospace = false, ConsoleLogType type = ConsoleLogType.Log)
	{
		if (ServerStatic.IsDedicated)
		{
			ServerConsole.AddLog(text, Misc.ClosestConsoleColor(c));
		}
	}

	public static GameObject FindConnectedRoot(NetworkConnection conn)
	{
		try
		{
			GameObject gameObject = conn.identity.gameObject;
			if (gameObject.CompareTag("Player"))
			{
				return gameObject;
			}
		}
		catch
		{
			return null;
		}
		return null;
	}

	internal string TypeCommand(string cmd, CommandSender sender = null)
	{
		if (sender == null)
		{
			sender = ServerConsole.Scs;
		}
		if (cmd.StartsWith(".", StringComparison.Ordinal) && cmd.Length > 1)
		{
			if (!NetworkClient.active && !NetworkServer.active)
			{
				Console.AddLog("You must be connected to a server to use this command.", Color.red);
				return "You must be connected to a server to use remote admin commands!";
			}
			string text = cmd;
			string text2 = text.Substring(1, text.Length - 1);
			string text3 = "Sending command to server: " + text2;
			sender?.Print(text3, ConsoleColor.Green);
			ReferenceHub.LocalHub.gameConsoleTransmission.SendToServer(text2);
			return text3;
		}
		bool flag = cmd.StartsWith("@", StringComparison.Ordinal);
		if ((cmd.StartsWith("/", StringComparison.Ordinal) || flag) && cmd.Length > 1)
		{
			string text4;
			if (!flag)
			{
				string text = cmd;
				text4 = text.Substring(1, text.Length - 1);
			}
			else
			{
				text4 = cmd;
			}
			string text5 = text4;
			if (!flag)
			{
				text5 = text5.TrimStart('$');
				if (string.IsNullOrEmpty(text5))
				{
					sender?.Print("Command cant be empty!", ConsoleColor.Green);
					return "Command cant be empty!";
				}
			}
			if (NetworkServer.active)
			{
				if (!flag)
				{
					return CommandProcessor.ProcessQuery(text5, sender);
				}
				string text = text5;
				CommandProcessor.ProcessAdminChat(text.Substring(1, text.Length - 1), sender);
				return null;
			}
		}
		string[] array = cmd.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
		cmd = array[0];
		ICommand command;
		bool flag2 = this.ConsoleCommandHandler.TryGetCommand(cmd, out command);
		ArraySegment<string> arguments = array.Segment(1);
		CommandExecutingEventArgs e = new CommandExecutingEventArgs(sender, CommandType.Console, flag2, command, arguments);
		ServerEvents.OnCommandExecuting(e);
		if (!e.IsAllowed)
		{
			return null;
		}
		arguments = e.Arguments;
		sender = e.Sender;
		command = e.Command;
		if (!flag2)
		{
			string text6 = "Command " + cmd + " does not exist!";
			sender?.Print(text6, ConsoleColor.DarkYellow, new Color32(byte.MaxValue, 180, 0, byte.MaxValue));
			return text6;
		}
		try
		{
			bool successful = command.Execute(array.Segment(1), sender, out var response);
			response = Misc.CloseAllRichTextTags(response);
			CommandExecutedEventArgs e2 = new CommandExecutedEventArgs(sender, CommandType.Console, command, arguments, successful, response);
			ServerEvents.OnCommandExecuted(e2);
			response = e2.Response;
			successful = e2.ExecutedSuccessfully;
			if (string.IsNullOrWhiteSpace(response))
			{
				return null;
			}
			sender?.Print(response, successful ? ConsoleColor.Green : ConsoleColor.Red);
			return response;
		}
		catch (Exception ex)
		{
			string response2 = "Command execution failed! Error: " + Misc.RemoveStacktraceZeroes(ex.ToString());
			CommandExecutedEventArgs e3 = new CommandExecutedEventArgs(sender, CommandType.Console, command, arguments, successful: false, response2);
			ServerEvents.OnCommandExecuted(e3);
			response2 = e3.Response;
			sender?.Print(response2, ConsoleColor.Red);
			return response2;
		}
	}

	public void ProceedButton()
	{
	}

	protected override void OnToggled()
	{
		base.OnToggled();
	}

	private void OnApplicationQuit()
	{
		Shutdown.Quit(quit: false);
	}
}
