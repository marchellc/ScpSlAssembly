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

namespace GameCore
{
	public class Console : SimpleToggleableMenu
	{
		public static Console singleton { get; private set; }

		protected override void Awake()
		{
			base.Awake();
			global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			if (Console.singleton == null)
			{
				Console.singleton = this;
				return;
			}
			global::UnityEngine.Object.DestroyImmediate(base.gameObject);
		}

		private void Start()
		{
			SceneManager.sceneLoaded += this.OnSceneLoaded;
			Console.AddLog("Hi there! Initializing console...", new Color32(0, byte.MaxValue, 0, byte.MaxValue), false, Console.ConsoleLogType.Log);
			Console.AddLog("Done! Type 'help' to print the list of available commands.", new Color32(0, byte.MaxValue, 0, byte.MaxValue), false, Console.ConsoleLogType.Log);
			CentralAuthManager.InitAuth();
			if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-tdm", StringComparison.OrdinalIgnoreCase)))
			{
				Console.TranslationDebugMode = true;
				Console.AddLog("Translation debug mode has been enabled (startup argument).", new Color32(0, byte.MaxValue, 0, byte.MaxValue), false, Console.ConsoleLogType.Log);
				return;
			}
			Console.TranslationDebugMode = false;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Console.AddLog(string.Concat(new string[] { "Scene Manager: Loaded scene '", scene.name, "' [", scene.path, "]" }), new Color32(0, byte.MaxValue, 0, byte.MaxValue), false, Console.ConsoleLogType.Log);
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
			Color32 color;
			if (ConsoleDebugMode.CheckImportance(debugKey, importance, out color))
			{
				Console.AddLog("[DEBUG_" + debugKey + "] " + message, color, nospace, Console.ConsoleLogType.Log);
			}
		}

		public static void AddLog(string text, Color c, bool nospace = false, Console.ConsoleLogType type = Console.ConsoleLogType.Log)
		{
			if (ServerStatic.IsDedicated)
			{
				ServerConsole.AddLog(text, Misc.ClosestConsoleColor(c, true), false);
				return;
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
					Console.AddLog("You must be connected to a server to use this command.", Color.red, false, Console.ConsoleLogType.Log);
					return "You must be connected to a server to use remote admin commands!";
				}
				string text = cmd;
				int length = text.Length;
				int num = 1;
				int num2 = length - num;
				string text2 = text.Substring(num, num2);
				string text3 = "Sending command to server: " + text2;
				if (sender != null)
				{
					sender.Print(text3, ConsoleColor.Green);
				}
				ReferenceHub.LocalHub.gameConsoleTransmission.SendToServer(text2);
				return text3;
			}
			else
			{
				bool flag = cmd.StartsWith("@", StringComparison.Ordinal);
				if ((cmd.StartsWith("/", StringComparison.Ordinal) || flag) && cmd.Length > 1)
				{
					string text5;
					if (!flag)
					{
						string text4 = cmd;
						int length2 = text4.Length;
						int num2 = 1;
						int num = length2 - num2;
						text5 = text4.Substring(num2, num);
					}
					else
					{
						text5 = cmd;
					}
					string text6 = text5;
					if (!flag)
					{
						text6 = text6.TrimStart('$');
						if (string.IsNullOrEmpty(text6))
						{
							if (sender != null)
							{
								sender.Print("Command cant be empty!", ConsoleColor.Green);
							}
							return "Command cant be empty!";
						}
					}
					if (NetworkServer.active)
					{
						if (!flag)
						{
							return CommandProcessor.ProcessQuery(text6, sender);
						}
						string text7 = text6;
						int length3 = text7.Length;
						int num = 1;
						int num2 = length3 - num;
						CommandProcessor.ProcessAdminChat(text7.Substring(num, num2), sender);
						return null;
					}
				}
				string[] array = cmd.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
				cmd = array[0];
				ICommand command;
				bool flag2 = this.ConsoleCommandHandler.TryGetCommand(cmd, out command);
				ArraySegment<string> arraySegment = array.Segment(1);
				CommandExecutingEventArgs commandExecutingEventArgs = new CommandExecutingEventArgs(sender, CommandType.Console, flag2, command, arraySegment);
				ServerEvents.OnCommandExecuting(commandExecutingEventArgs);
				if (!commandExecutingEventArgs.IsAllowed)
				{
					return null;
				}
				arraySegment = commandExecutingEventArgs.Arguments;
				sender = commandExecutingEventArgs.Sender;
				command = commandExecutingEventArgs.Command;
				if (!flag2)
				{
					string text8 = "Command " + cmd + " does not exist!";
					if (sender != null)
					{
						sender.Print(text8, ConsoleColor.DarkYellow, new Color32(byte.MaxValue, 180, 0, byte.MaxValue));
					}
					return text8;
				}
				string text10;
				try
				{
					string text9;
					bool flag3 = command.Execute(array.Segment(1), sender, out text9);
					text9 = Misc.CloseAllRichTextTags(text9);
					CommandExecutedEventArgs commandExecutedEventArgs = new CommandExecutedEventArgs(sender, CommandType.Console, command, arraySegment, flag3, text9);
					ServerEvents.OnCommandExecuted(commandExecutedEventArgs);
					text9 = commandExecutedEventArgs.Response;
					flag3 = commandExecutedEventArgs.ExecutedSuccessfully;
					if (string.IsNullOrWhiteSpace(text9))
					{
						text10 = null;
					}
					else
					{
						if (sender != null)
						{
							sender.Print(text9, flag3 ? ConsoleColor.Green : ConsoleColor.Red);
						}
						text10 = text9;
					}
				}
				catch (Exception ex)
				{
					string text11 = "Command execution failed! Error: " + Misc.RemoveStacktraceZeroes(ex.ToString());
					CommandExecutedEventArgs commandExecutedEventArgs2 = new CommandExecutedEventArgs(sender, CommandType.Console, command, arraySegment, false, text11);
					ServerEvents.OnCommandExecuted(commandExecutedEventArgs2);
					text11 = commandExecutedEventArgs2.Response;
					if (sender != null)
					{
						sender.Print(text11, ConsoleColor.Red);
					}
					text10 = text11;
				}
				return text10;
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
			Shutdown.Quit(false, false);
		}

		public CommandHint[] hints;

		public readonly GameConsoleCommandHandler ConsoleCommandHandler = GameConsoleCommandHandler.Create();

		internal static bool TranslationDebugMode;

		internal static AsymmetricKeyParameter _publicKey;

		private string _content;

		public enum ConsoleLogType
		{
			DoNotLog,
			Log,
			Warning,
			Error
		}
	}
}
