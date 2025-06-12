using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Authenticator;
using CentralAuth;
using Cryptography;
using GameCore;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib;
using NorthwoodLib.Pools;
using Org.BouncyCastle.Crypto;
using PlayerRoles;
using Respawning.NamingRules;
using ServerOutput;
using Steam;
using UnityEngine;
using Utils.CommandInterpolation;

public class ServerConsole : MonoBehaviour, IDisposable
{
	public static string ServerName = string.Empty;

	private static readonly Func<float, float> _roundNormal = Mathf.Round;

	private static readonly Func<float, float> _roundCeil = Mathf.Ceil;

	private static readonly Func<float, float> _roundFloor = Mathf.Floor;

	private static readonly Func<float, float, float> _pow = Mathf.Pow;

	private static bool _disposing;

	public static Process ConsoleProcess;

	public static string Password;

	public static string Ip;

	public static AsymmetricKeyParameter PublicKey;

	public static bool Update;

	public static bool ScheduleTokenRefresh;

	public static bool FriendlyFire = false;

	public static bool WhiteListEnabled = false;

	public static bool AccessRestriction = false;

	public static bool RateLimitKick;

	internal static bool EnforceSameIp;

	internal static bool SkipEnforcementForLocalAddresses;

	internal static bool TransparentlyModdedServerConfig;

	private static bool _printedNotVerifiedMessage;

	private static bool _emailSet;

	private static float _heartbeatTimer;

	public static readonly ServerConsoleSender Scs = new ServerConsoleSender();

	public static ushort PortOverride;

	private static readonly ConcurrentBag<ReferenceHub> NewPlayers = new ConcurrentBag<ReferenceHub>();

	private static int _playersAmount;

	public static readonly ConcurrentDictionary<string, IOutput> ConsoleOutputs = new ConcurrentDictionary<string, IOutput>();

	internal static readonly ConcurrentQueue<string> PrompterQueue = new ConcurrentQueue<string>();

	private static readonly PlayerListSerialized PlayersListRaw = new PlayerListSerialized(new List<string>());

	private static string _verificationPlayersList = string.Empty;

	private static Thread _checkProcessThread;

	private static Thread _refreshPublicKeyThread;

	private static Thread _refreshPublicKeyOnceThread;

	private static Thread _verificationRequestThread;

	private static readonly Regex _sizeRegex = new Regex("(<size=(.*?)<\\/size>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex _colorRegex = new Regex("(<color=(.*?)<\\/color>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static ServerConsole Singleton { get; private set; }

	public InterpolatedCommandFormatter NameFormatter { get; private set; }

	internal static ushort PortToReport
	{
		get
		{
			if (ServerConsole.PortOverride != 0)
			{
				return ServerConsole.PortOverride;
			}
			return LiteNetLib4MirrorTransport.Singleton.port;
		}
	}

	public static void ReloadServerName()
	{
		ServerConsole.ServerName = ConfigFile.ServerConfig.GetString("server_name", "My Server Name");
	}

	public void Dispose()
	{
		ServerConsole._disposing = true;
		ServerStatic.ServerOutput?.Dispose();
		if (ServerConsole._checkProcessThread != null && ServerConsole._checkProcessThread.IsAlive)
		{
			ServerConsole._checkProcessThread.Abort();
		}
		if (ServerConsole._verificationRequestThread != null && ServerConsole._verificationRequestThread.IsAlive)
		{
			ServerConsole._verificationRequestThread.Abort();
		}
		if (ServerConsole._refreshPublicKeyThread != null && ServerConsole._refreshPublicKeyThread.IsAlive)
		{
			ServerConsole._refreshPublicKeyThread.Abort();
		}
		if (ServerConsole._refreshPublicKeyOnceThread != null && ServerConsole._refreshPublicKeyOnceThread.IsAlive)
		{
			ServerConsole._refreshPublicKeyOnceThread.Abort();
		}
		if (ServerConsole._verificationRequestThread != null && ServerConsole._verificationRequestThread.IsAlive)
		{
			ServerConsole._verificationRequestThread.Abort();
		}
	}

	[DllImport("libc", EntryPoint = "getuid")]
	private static extern uint GetUserId();

	private static void CheckRoot()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && ServerConsole.GetUserId() == 0)
		{
			GameCore.Console.AddLog("Running the game as ROOT is NOT recommended, please create a separate user!", Color.red);
		}
	}

	private void Start()
	{
		ServerConsole.CheckRoot();
		this.NameFormatter = new InterpolatedCommandFormatter
		{
			StartClosure = '{',
			EndClosure = '}',
			Escape = '\\',
			ArgumentSplitter = ',',
			Commands = new Dictionary<string, Func<List<string>, string>>
			{
				{
					"ip",
					(List<string> args) => ServerConsole.Ip
				},
				{
					"port",
					(List<string> args) => LiteNetLib4MirrorTransport.Singleton.port.ToString()
				},
				{
					"number",
					(List<string> args) => (LiteNetLib4MirrorTransport.Singleton.port - 7776).ToString()
				},
				{
					"version",
					(List<string> args) => GameCore.Version.VersionString
				},
				{
					"player_count",
					(List<string> args) => ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Host).ToString()
				},
				{
					"full_player_count",
					delegate(List<string> args)
					{
						int playerCount = ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Host);
						return (playerCount == CustomNetworkManager.TypedSingleton.ReservedMaxPlayers) ? (args.Count switch
						{
							1 => "FULL", 
							2 => this.NameFormatter.ProcessExpression(args[1]), 
							_ => throw new ArgumentOutOfRangeException("args", args, "Invalid arguments. Use: full_player_count OR full_player_count,[full]"), 
						}) : $"{playerCount}/{CustomNetworkManager.TypedSingleton.ReservedMaxPlayers}";
					}
				},
				{
					"max_players",
					(List<string> args) => CustomNetworkManager.TypedSingleton.ReservedMaxPlayers.ToString()
				},
				{
					"round_duration_minutes",
					(List<string> args) => RoundStart.RoundLength.Minutes.ToString("00")
				},
				{
					"round_duration_seconds",
					(List<string> args) => RoundStart.RoundLength.Seconds.ToString("00")
				},
				{
					"kills",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.Kills.ToString(), intValue: true)
				},
				{
					"alive_role",
					delegate(List<string> args)
					{
						if (args.Count != 2)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use: alive_role,[role ID]");
						}
						if (!Enum.TryParse<RoleTypeId>(this.NameFormatter.ProcessExpression(args[1]), out var role))
						{
							throw new CommandInputException("role ID", args[1], "Could not parse.");
						}
						return ServerConsole.GetRoundInfo((RoundSummary s) => s.CountRole(role).ToString(), intValue: true);
					}
				},
				{
					"alive_team",
					delegate(List<string> args)
					{
						if (args.Count != 2)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use: alive_team,[team ID]");
						}
						if (!Enum.TryParse<Team>(this.NameFormatter.ProcessExpression(args[1]), out var team))
						{
							throw new CommandInputException("team ID", args[1], "Could not parse.");
						}
						return ServerConsole.GetRoundInfo((RoundSummary s) => s.CountTeam(team).ToString(), intValue: true);
					}
				},
				{
					"zombies_recalled",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.ChangedIntoZombies.ToString(), intValue: true)
				},
				{
					"scp_counter",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => $"{summary.CountTeam(Team.SCPs) - summary.CountRole(RoleTypeId.Scp0492)}/{summary.classlistStart.scps_except_zombies}")
				},
				{
					"scp_start",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.scps_except_zombies.ToString(), intValue: true)
				},
				{
					"scp_killed",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => (summary.classlistStart.scps_except_zombies - summary.CountTeam(Team.SCPs) - summary.CountRole(RoleTypeId.Scp0492)).ToString(), intValue: true)
				},
				{
					"scp_kills",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.KilledBySCPs.ToString(), intValue: true)
				},
				{
					"classd_counter",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => $"{RoundSummary.EscapedClassD}/{summary.classlistStart.class_ds}", intValue: true)
				},
				{
					"classd_start",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.class_ds.ToString(), intValue: true)
				},
				{
					"classd_escaped",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.EscapedClassD.ToString(), intValue: true)
				},
				{
					"scientist_counter",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => $"{RoundSummary.EscapedScientists}/{summary.classlistStart.scientists}")
				},
				{
					"scientist_start",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.scientists.ToString(), intValue: true)
				},
				{
					"scientist_escaped",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.EscapedScientists.ToString(), intValue: true)
				},
				{
					"mtf_respawns",
					(List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => (NamingRulesManager.GeneratedNames.TryGetValue(Team.FoundationForces, out var value) ? (value.Count - 1) : 0).ToString(), intValue: true)
				},
				{
					"warhead_detonated",
					(List<string> args) => args.Count switch
					{
						1 => ServerConsole.GetRoundInfo((RoundSummary s) => (!AlphaWarheadController.Detonated) ? string.Empty : "☢ WARHEAD DETONATED ☢"), 
						3 => ServerConsole.GetRoundInfo((RoundSummary s) => this.NameFormatter.ProcessExpression(args[AlphaWarheadController.Detonated ? 1 : 2])), 
						_ => throw new CommandInputException("args", args, "Invalid arguments. Use: warhead_detonated OR warhead_detonated,[detonated],[undetonated]"), 
					}
				},
				{
					"random",
					delegate(List<string> args)
					{
						float result;
						float result2;
						switch (args.Count)
						{
						case 2:
						{
							result = 0f;
							string text2 = this.NameFormatter.ProcessExpression(args[1]);
							if (!float.TryParse(text2, out result2))
							{
								throw new CommandInputException("max", text2, "Could not parse.");
							}
							break;
						}
						case 3:
						{
							string text = this.NameFormatter.ProcessExpression(args[1]);
							if (!float.TryParse(text, out result))
							{
								throw new CommandInputException("min", text, "Could not parse.");
							}
							string text2 = this.NameFormatter.ProcessExpression(args[2]);
							if (!float.TryParse(text2, out result2))
							{
								throw new CommandInputException("max", text2, "Could not parse.");
							}
							break;
						}
						default:
							throw new CommandInputException("args", args, "Invalid arguments. Use: random,[max] OR random,[min],[max]");
						}
						return UnityEngine.Random.Range(result, result2).ToString();
					}
				},
				{
					"random_list",
					delegate(List<string> args)
					{
						if (args.Count < 3)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use: random_list,[entry 1],[entry 2]...");
						}
						return this.NameFormatter.ProcessExpression(args[UnityEngine.Random.Range(1, args.Count)]);
					}
				},
				{
					"constant_e",
					(List<string> args) => MathF.E.ToString()
				},
				{
					"constant_pi",
					(List<string> args) => MathF.PI.ToString()
				},
				{
					"add",
					(List<string> args) => this.StandardizedFloatComparison("add", args, (float a, float b) => a + b)
				},
				{
					"subtract",
					(List<string> args) => this.StandardizedFloatComparison("subtract", args, (float a, float b) => a - b)
				},
				{
					"multiply",
					(List<string> args) => this.StandardizedFloatComparison("multiply", args, (float a, float b) => a * b)
				},
				{
					"division",
					(List<string> args) => this.StandardizedFloatComparison("division", args, (float a, float b) => a / b)
				},
				{
					"power",
					(List<string> args) => this.StandardizedFloatComparison("power", args, ServerConsole._pow)
				},
				{
					"log",
					delegate(List<string> args)
					{
						float result;
						float result2;
						switch (args.Count)
						{
						case 2:
						{
							string text = this.NameFormatter.ProcessExpression(args[1]);
							if (!float.TryParse(text, out result))
							{
								throw new CommandInputException("value", text, "Could not parse.");
							}
							result2 = 10f;
							break;
						}
						case 3:
						{
							string text = this.NameFormatter.ProcessExpression(args[1]);
							if (!float.TryParse(text, out result))
							{
								throw new CommandInputException("value", text, "Could not parse.");
							}
							string text2 = this.NameFormatter.ProcessExpression(args[2]);
							if (!float.TryParse(text2, out result2))
							{
								throw new CommandInputException("base", text2, "Could not parse.");
							}
							break;
						}
						default:
							throw new CommandInputException("args", args, "Invalid arguments. Use log,[value] OR log,[value],[base]");
						}
						return Mathf.Log(result, result2).ToString();
					}
				},
				{
					"ln",
					delegate(List<string> args)
					{
						if (args.Count < 2)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use ln,[value]");
						}
						string text = this.NameFormatter.ProcessExpression(args[1]);
						if (!float.TryParse(text, out var result))
						{
							throw new CommandInputException("value", text, "Error parsing value.");
						}
						return Mathf.Log(result).ToString();
					}
				},
				{
					"round",
					(List<string> args) => this.StandardizedFloatRound("round", args, ServerConsole._roundNormal)
				},
				{
					"round_up",
					(List<string> args) => this.StandardizedFloatRound("round_up", args, ServerConsole._roundCeil)
				},
				{
					"round_down",
					(List<string> args) => this.StandardizedFloatRound("round_down", args, ServerConsole._roundFloor)
				},
				{
					"equals",
					delegate(List<string> args)
					{
						if (args.Count != 3)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use equals,[object A],[object B]");
						}
						return (args[1] == args[2]).ToString();
					}
				},
				{
					"greater",
					(List<string> args) => this.StandardizedFloatComparison("greater", args, (float a, float b) => a > b)
				},
				{
					"lesser",
					(List<string> args) => this.StandardizedFloatComparison("lesser", args, (float a, float b) => a < b)
				},
				{
					"greater_or_equal",
					(List<string> args) => this.StandardizedFloatComparison("greater_or_equal", args, (float a, float b) => a >= b)
				},
				{
					"lesser_or_equal",
					(List<string> args) => this.StandardizedFloatComparison("lesser_or_equal", args, (float a, float b) => a <= b)
				},
				{
					"not",
					delegate(List<string> args)
					{
						if (args.Count != 2)
						{
							throw new CommandInputException("args", args, "Invalid arguments. Use not,[value]");
						}
						string text = this.NameFormatter.ProcessExpression(args[1]);
						if (!bool.TryParse(text, out var result))
						{
							throw new CommandInputException("value", text, "Error parsing value.");
						}
						return (!result).ToString();
					}
				},
				{
					"or",
					(List<string> args) => this.StandardizedBoolComparison("or", args, (bool a, bool b) => a || b)
				},
				{
					"xor",
					(List<string> args) => this.StandardizedBoolComparison("xor", args, (bool a, bool b) => a ^ b)
				},
				{
					"and",
					(List<string> args) => this.StandardizedBoolComparison("and", args, (bool a, bool b) => a && b)
				},
				{
					"if",
					delegate(List<string> args)
					{
						string text;
						string text2;
						switch (args.Count)
						{
						case 3:
							text = args[2];
							text2 = string.Empty;
							break;
						case 4:
							text = args[2];
							text2 = args[3];
							break;
						default:
							throw new CommandInputException("args", args, "Invalid arguments. Use if,[condition],[action] OR if,[condition],[action],[else action]");
						}
						string text3 = this.NameFormatter.ProcessExpression(args[1]);
						if (!bool.TryParse(text3, out var result))
						{
							throw new CommandInputException("condition", text3, "Could not parse.");
						}
						return this.NameFormatter.ProcessExpression(result ? text : text2);
					}
				}
			}
		};
		PlayerAuthenticationManager.OnInstanceModeChanged += HandlePlayerJoin;
		ReferenceHub.OnPlayerRemoved += delegate
		{
			ServerConsole.RefreshOnlinePlayers();
		};
		if (ServerStatic.IsDedicated && ServerStatic.ProcessIdPassed)
		{
			ServerConsole._checkProcessThread = new Thread(CheckProcess)
			{
				Priority = System.Threading.ThreadPriority.Lowest,
				IsBackground = true,
				Name = "Dedicated server console running check"
			};
			ServerConsole._checkProcessThread.Start();
		}
	}

	private static void HandlePlayerJoin(ReferenceHub rh, ClientInstanceMode mode)
	{
		if (mode == ClientInstanceMode.ReadyClient)
		{
			ServerConsole.NewPlayers.Add(rh);
			ServerConsole.RefreshOnlinePlayers();
		}
	}

	private void FixedUpdate()
	{
		if (ServerStatic.EnableConsoleHeartbeat)
		{
			ServerConsole._heartbeatTimer += Time.fixedUnscaledDeltaTime;
			if (ServerConsole._heartbeatTimer >= 5f)
			{
				ServerConsole._heartbeatTimer = 0f;
				ServerConsole.AddOutputEntry(default(HeartbeatEntry));
			}
		}
		string result;
		while (ServerConsole.PrompterQueue.TryDequeue(out result))
		{
			if (!string.IsNullOrWhiteSpace(result))
			{
				ServerConsole.EnterCommand(result, ServerConsole.Scs);
			}
		}
	}

	private static void RefreshOnlinePlayers()
	{
		try
		{
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				ClientInstanceMode mode = allHub.Mode;
				if ((mode == ClientInstanceMode.ReadyClient || mode == ClientInstanceMode.Host) && !string.IsNullOrEmpty(allHub.authManager.UserId) && (!allHub.isLocalPlayer || !ServerStatic.IsDedicated))
				{
					ServerConsole.PlayersListRaw.objects.Add(allHub.authManager.UserId);
				}
			}
			ServerConsole._verificationPlayersList = JsonSerialize.ToJson(ServerConsole.PlayersListRaw);
			ServerConsole._playersAmount = ServerConsole.PlayersListRaw.objects.Count;
			SteamServerInfo.OnlinePlayers = ServerConsole._playersAmount;
			ServerConsole.PlayersListRaw.objects.Clear();
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("[VERIFICATION] Exception in Players Online processing: " + ex.Message);
			ServerConsole.AddLog(ex.StackTrace);
		}
	}

	private string StandardizedBoolComparison<T>(string source, IReadOnlyList<string> args, Func<bool, bool, T> comparison)
	{
		bool result;
		return this.StandardizedComparison(source, args, (string arg) => (success: bool.TryParse(arg, out result), value: result), comparison);
	}

	private string StandardizedFloatComparison<T>(string source, IReadOnlyList<string> args, Func<float, float, T> comparison)
	{
		float result;
		return this.StandardizedComparison(source, args, (string arg) => (success: float.TryParse(arg, out result), value: result), comparison);
	}

	private string StandardizedComparison<TArg, TResult>(string source, IReadOnlyList<string> args, Func<string, (bool success, TArg value)> parse, Func<TArg, TArg, TResult> comparison)
	{
		if (args.Count != 3)
		{
			throw new CommandInputException("args", args, "Invalid arguments. Use " + source + ",[value A],[value B]");
		}
		string arg = this.NameFormatter.ProcessExpression(args[1]);
		var (flag, arg2) = parse(arg);
		if (!flag)
		{
			throw new CommandInputException("value A", args[1], "Could not parse.");
		}
		string text = this.NameFormatter.ProcessExpression(args[2]);
		var (flag2, arg3) = parse(text);
		if (!flag2)
		{
			throw new CommandInputException("value B", text, "Could not parse.");
		}
		return comparison(arg2, arg3).ToString();
	}

	private string StandardizedFloatRound(string source, IReadOnlyList<string> args, Func<float, float> rounder)
	{
		float result;
		int result2;
		switch (args.Count)
		{
		case 2:
		{
			string text = this.NameFormatter.ProcessExpression(args[1]);
			if (!float.TryParse(text, out result))
			{
				throw new CommandInputException("value", text, "Could not parse.");
			}
			result2 = 0;
			break;
		}
		case 3:
		{
			string text = this.NameFormatter.ProcessExpression(args[1]);
			if (!float.TryParse(text, out result))
			{
				throw new CommandInputException("value", text, "Could not parse.");
			}
			string text2 = this.NameFormatter.ProcessExpression(args[1]);
			if (!int.TryParse(text2, out result2))
			{
				throw new CommandInputException("precision", text2, "Could not parse.");
			}
			break;
		}
		default:
			throw new CommandInputException("args", args, "Invalid arguments. Use " + source + ",[value] OR " + source + ",[value],[precision]");
		}
		float num = Mathf.Pow(10f, result2);
		return (rounder(result * num) / num).ToString(CultureInfo.InvariantCulture);
	}

	private static string GetRoundInfo(Func<RoundSummary, string> getter, bool intValue = false)
	{
		if (!(RoundSummary.singleton == null))
		{
			return getter(RoundSummary.singleton);
		}
		if (!intValue)
		{
			return "-";
		}
		return "-1";
	}

	public string RefreshServerName()
	{
		return this.NameFormatter.ProcessExpression(ServerConsole.ServerName);
	}

	public string RefreshServerNameSafe()
	{
		if (this.NameFormatter.TryProcessExpression(ServerConsole.ServerName, "server name", out var result))
		{
			SteamServerInfo.ServerName = Regex.Replace(result, "<[^>]*>", string.Empty);
			return result;
		}
		ServerConsole.AddLog(result);
		return "Command errored";
	}

	private void Awake()
	{
		ServerConsole.Singleton = this;
	}

	private static void CheckProcess()
	{
		while (!ServerConsole._disposing)
		{
			Thread.Sleep(4000);
			if (ServerConsole.ConsoleProcess == null || ServerConsole.ConsoleProcess.HasExited)
			{
				ServerConsole.ConsoleProcess?.Dispose();
				ServerConsole.ConsoleProcess = null;
				ServerConsole.DisposeStatic();
			}
		}
	}

	public void OnDestroy()
	{
		this.Dispose();
	}

	public void OnApplicationQuit()
	{
		this.Dispose();
	}

	public static void DisposeStatic()
	{
		ServerConsole.Singleton.Dispose();
	}

	public static void AddLog(string q, ConsoleColor color = ConsoleColor.Gray, bool hideFromOutputs = false)
	{
		ServerConsole.PrintFormattedString(q, color);
		if (!hideFromOutputs)
		{
			ServerConsole.PrintOnOutputs(q, color);
		}
	}

	public static void AddOutputEntry(IOutputEntry entry)
	{
		ServerStatic.ServerOutput?.AddOutput(entry);
		if (entry is TextOutputEntry)
		{
			ServerConsole.PrintOnOutputs(entry.ToString(), ConsoleColor.Gray);
		}
	}

	public static void Disconnect(GameObject player, string message)
	{
		if (player == null)
		{
			return;
		}
		NetworkBehaviour component = player.GetComponent<NetworkBehaviour>();
		if (!(component == null))
		{
			CharacterClassManager component2 = player.GetComponent<CharacterClassManager>();
			if (component2 == null)
			{
				component.connectionToClient.Disconnect();
			}
			else
			{
				component2.DisconnectClient(component.connectionToClient, message);
			}
		}
	}

	public static void Disconnect(NetworkConnection conn, string message)
	{
		GameObject gameObject = GameCore.Console.FindConnectedRoot(conn);
		if (gameObject == null)
		{
			conn.Disconnect();
		}
		else
		{
			ServerConsole.Disconnect(gameObject, message);
		}
	}

	public static string ColorText(string text, ConsoleColor color)
	{
		return "<color=" + ServerConsole.ConsoleColorToHex(color) + ">" + text + "</color>";
	}

	public static void ColorDebugLog(string text, ConsoleColor color)
	{
		UnityEngine.Debug.Log(ServerConsole.ColorText(text, color), null);
	}

	public static void PrintFormattedString(string text, ConsoleColor defaultColor)
	{
		text = ServerConsole._sizeRegex.Replace(text, "").Trim();
		string[] array = ServerConsole._colorRegex.Split(text);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i];
			if (!text2.ToLowerInvariant().StartsWith("<color=", StringComparison.Ordinal))
			{
				ServerStatic.ServerOutput?.AddLog(text2, defaultColor);
				continue;
			}
			string text3 = text2.Substring(7);
			text3 = text3.Substring(0, text3.IndexOf('>')).Replace("\"", "").Replace("'", "");
			string text4 = text2.Substring(text2.IndexOf('>') + 1);
			text4 = text4.Substring(0, text4.IndexOf('<'));
			bool flag = text3.StartsWith("#", StringComparison.Ordinal);
			ConsoleColor color = defaultColor;
			ConsoleColor result;
			if (flag && Misc.TryParseColor(text3, out var color2))
			{
				color = Misc.ClosestConsoleColor(color2, excludeDark: false);
			}
			else if (!flag && Enum.TryParse<ConsoleColor>(text3, ignoreCase: true, out result))
			{
				color = result;
			}
			ServerStatic.ServerOutput?.AddLog(text4, color);
			i++;
		}
	}

	public static string ConsoleColorToHex(ConsoleColor color)
	{
		return color switch
		{
			ConsoleColor.Black => "#000000", 
			ConsoleColor.Blue => "#0078F8", 
			ConsoleColor.Cyan => "#00FFFF", 
			ConsoleColor.DarkBlue => "#0058F8", 
			ConsoleColor.DarkCyan => "#00B7EB", 
			ConsoleColor.DarkGray => "#787878", 
			ConsoleColor.DarkGreen => "#005800", 
			ConsoleColor.DarkMagenta => "#FF0090", 
			ConsoleColor.DarkRed => "#A80020", 
			ConsoleColor.DarkYellow => "#AC7C00", 
			ConsoleColor.Gray => "#D8D8D8", 
			ConsoleColor.Green => "#00B800", 
			ConsoleColor.Magenta => "#FF00FF", 
			ConsoleColor.Red => "#DC143C", 
			ConsoleColor.White => "#FCFCFC", 
			ConsoleColor.Yellow => "#F8B800", 
			_ => "#6844FC", 
		};
	}

	public static Color ConsoleColorToColor(ConsoleColor color)
	{
		return color switch
		{
			ConsoleColor.Black => Color.black, 
			ConsoleColor.Blue => Color.blue, 
			ConsoleColor.Cyan => Color.cyan, 
			ConsoleColor.DarkBlue => new Color(0f, 88f, 248f), 
			ConsoleColor.DarkCyan => new Color(0f, 183f, 235f), 
			ConsoleColor.DarkGray => Color.gray, 
			ConsoleColor.DarkGreen => new Color(0f, 88f, 0f), 
			ConsoleColor.DarkMagenta => new Color(255f, 0f, 144f), 
			ConsoleColor.DarkRed => new Color(168f, 0f, 32f), 
			ConsoleColor.DarkYellow => new Color(172f, 124f, 0f), 
			ConsoleColor.Gray => new Color(216f, 216f, 216f), 
			ConsoleColor.Green => Color.green, 
			ConsoleColor.Magenta => Color.magenta, 
			ConsoleColor.Red => Color.red, 
			ConsoleColor.White => new Color(252f, 252f, 252f), 
			ConsoleColor.Yellow => Color.yellow, 
			_ => new Color(104f, 68f, 252f), 
		};
	}

	public static string EnterCommand(string cmds, CommandSender sender = null)
	{
		string[] args = cmds.Split(' ');
		if (args.Length == 0)
		{
			return string.Empty;
		}
		if (sender == null)
		{
			sender = ServerConsole.Scs;
		}
		string cmd = args[0];
		if (cmd.StartsWith("!", StringComparison.Ordinal) && cmd.Length > 1)
		{
			if (cmd.StartsWith("!verify", StringComparison.OrdinalIgnoreCase) && !ServerConsole._emailSet)
			{
				return "You have to set the contact email address (\"contact_email\" key in the gameplay config) before running this command!";
			}
			Thread thread = new Thread((ThreadStart)delegate
			{
				ServerConsole.RunCentralServerCommand(cmd.Substring(1).ToLower(), (args.Length == 1) ? "" : args.Skip(1).Aggregate((string current, string next) => current + " " + next));
			});
			thread.IsBackground = true;
			thread.Priority = System.Threading.ThreadPriority.AboveNormal;
			thread.Name = "SCP:SL Central server command execution";
			thread.Start();
			return "Sending command to central servers...";
		}
		return GameCore.Console.singleton.TypeCommand(cmds, sender ?? ServerConsole.Scs);
	}

	public void RunServer()
	{
		Thread verificationRequestThread = ServerConsole._verificationRequestThread;
		if (verificationRequestThread == null || !verificationRequestThread.IsAlive)
		{
			ServerConsole._verificationRequestThread = new Thread(RefreshServerData)
			{
				IsBackground = true,
				Priority = System.Threading.ThreadPriority.AboveNormal,
				Name = "SCP:SL Server list thread"
			};
			ServerConsole._verificationRequestThread.Start();
		}
	}

	internal static void RunRefreshPublicKey()
	{
		Thread refreshPublicKeyThread = ServerConsole._refreshPublicKeyThread;
		if (refreshPublicKeyThread == null || !refreshPublicKeyThread.IsAlive)
		{
			ServerConsole._refreshPublicKeyThread = new Thread(RefreshPublicKey)
			{
				IsBackground = true,
				Priority = System.Threading.ThreadPriority.Normal,
				Name = "SCP:SL Public key refreshing"
			};
			ServerConsole._refreshPublicKeyThread.Start();
		}
	}

	internal static void RunRefreshPublicKeyOnce()
	{
		Thread refreshPublicKeyOnceThread = ServerConsole._refreshPublicKeyOnceThread;
		if (refreshPublicKeyOnceThread == null || !refreshPublicKeyOnceThread.IsAlive)
		{
			ServerConsole._refreshPublicKeyOnceThread = new Thread(RefreshPublicKeyOnce)
			{
				IsBackground = true,
				Priority = System.Threading.ThreadPriority.AboveNormal,
				Name = "SCP:SL Public key refreshing ON DEMAND"
			};
			ServerConsole._refreshPublicKeyOnceThread.Start();
		}
	}

	private static void RefreshPublicKey()
	{
		string text = CentralServerKeyCache.ReadCache();
		string text2 = string.Empty;
		string text3 = string.Empty;
		bool flag = true;
		if (!string.IsNullOrEmpty(text))
		{
			ServerConsole.PublicKey = ECDSA.PublicKeyFromString(text);
			text2 = Sha.HashToString(Sha.Sha256(ECDSA.KeyToString(ServerConsole.PublicKey)));
			ServerConsole.AddLog("Loaded central server public key from cache.");
			ServerConsole.AddLog("SHA256 of public key: " + text2);
		}
		ServerConsole.AddLog("Downloading public key from central server...");
		while (!ServerConsole._disposing)
		{
			try
			{
				PublicKeyResponse publicKeyResponse = JsonSerialize.FromJson<PublicKeyResponse>(HttpQuery.Get($"{CentralServer.StandardUrl}v5/publickey.php?major={GameCore.Version.Major}"));
				if (!ECDSA.Verify(publicKeyResponse.key, publicKeyResponse.signature, CentralServerKeyCache.MasterKey))
				{
					GameCore.Console.AddLog("Can't refresh central server public key - invalid signature!", Color.red);
					Thread.Sleep(360000);
					continue;
				}
				ServerConsole.PublicKey = ECDSA.PublicKeyFromString(publicKeyResponse.key);
				string text4 = Sha.HashToString(Sha.Sha256(ECDSA.KeyToString(ServerConsole.PublicKey)));
				if (text4 != text3)
				{
					text3 = text4;
					ServerConsole.AddLog("Downloaded public key from central server.");
					ServerConsole.AddLog("SHA256 of public key: " + text4);
					if (text4 != text2)
					{
						CentralServerKeyCache.SaveCache(publicKeyResponse.key, publicKeyResponse.signature);
					}
					else
					{
						ServerConsole.AddLog("SHA256 of cached key matches, no need to update cache.");
					}
				}
				else if (flag)
				{
					flag = false;
					ServerConsole.AddLog("Refreshed public key of central server - key hash not changed.");
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Can't refresh central server public key - " + ex.Message);
			}
			Thread.Sleep(360000);
		}
	}

	private static void RefreshPublicKeyOnce()
	{
		try
		{
			PublicKeyResponse publicKeyResponse = JsonSerialize.FromJson<PublicKeyResponse>(HttpQuery.Get($"{CentralServer.StandardUrl}v5/publickey.php?major={GameCore.Version.Major}"));
			if (!ECDSA.Verify(publicKeyResponse.key, publicKeyResponse.signature, CentralServerKeyCache.MasterKey))
			{
				GameCore.Console.AddLog("Can't refresh central server public key - invalid signature!", Color.red);
				return;
			}
			ServerConsole.PublicKey = ECDSA.PublicKeyFromString(publicKeyResponse.key);
			string text = Sha.HashToString(Sha.Sha256(ECDSA.KeyToString(ServerConsole.PublicKey)));
			ServerConsole.AddLog("Downloaded public key from central server.");
			ServerConsole.AddLog("SHA256 of public key: " + text);
			CentralServerKeyCache.SaveCache(publicKeyResponse.key, publicKeyResponse.signature);
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Can't refresh central server public key - " + ex.Message);
		}
	}

	private static void RunCentralServerCommand(string cmd, string args)
	{
		cmd = cmd.ToLower();
		List<string> list = new List<string>
		{
			"ip=" + ServerConsole.Ip,
			"port=" + ServerConsole.PortToReport,
			"cmd=" + StringUtils.Base64Encode(cmd),
			"args=" + StringUtils.Base64Encode(args)
		};
		if (!string.IsNullOrEmpty(ServerConsole.Password))
		{
			list.Add("passcode=" + ServerConsole.Password);
		}
		try
		{
			string text = HttpQuery.Post(CentralServer.MasterUrl + "centralcommands/" + cmd + ".php", HttpQuery.ToPostArgs(list));
			ServerConsole.AddLog("[" + cmd + "] " + text);
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Could not execute the central server command \"" + cmd + "\" - (LOCAL EXCEPTION) " + ex.Message, ConsoleColor.Red);
		}
	}

	internal static void RefreshEmailSetStatus()
	{
		ServerConsole._emailSet = !string.IsNullOrEmpty(ConfigFile.ServerConfig.GetString("contact_email"));
	}

	private void RefreshServerData()
	{
		bool flag = true;
		byte b = 0;
		ServerConsole.RefreshEmailSetStatus();
		ServerConsole.RefreshToken(init: true);
		while (!ServerConsole._disposing)
		{
			b++;
			if (!flag && string.IsNullOrEmpty(ServerConsole.Password) && b < 15)
			{
				if (b == 5 || b == 12 || ServerConsole.ScheduleTokenRefresh)
				{
					ServerConsole.RefreshToken();
				}
			}
			else
			{
				flag = false;
				ServerConsole.Update = ServerConsole.Update || b == 10;
				string text = string.Empty;
				try
				{
					int count = ServerConsole.NewPlayers.Count;
					int num = 0;
					List<AuthenticatorPlayerObject> list = ListPool<AuthenticatorPlayerObject>.Shared.Rent();
					while (!ServerConsole.NewPlayers.IsEmpty)
					{
						num++;
						if (num > count + 30)
						{
							break;
						}
						try
						{
							if (ServerConsole.NewPlayers.TryTake(out var result) && result != null)
							{
								string userId = result.authManager.UserId;
								string ip = ((result.authManager.connectionToClient == null || string.IsNullOrEmpty(result.authManager.connectionToClient.address)) ? "N/A" : result.authManager.connectionToClient.address);
								string requestIp = result.authManager.AuthenticationResponse.AuthToken.RequestIp;
								int asn = result.authManager.AuthenticationResponse.AuthToken.Asn;
								list.Add(new AuthenticatorPlayerObject(userId, ip, requestIp, asn.ToString(), result.authManager.AuthenticationResponse.AuthToken.Serial, result.authManager.AuthenticationResponse.AuthToken.VacSession));
							}
						}
						catch (Exception ex)
						{
							ServerConsole.AddLog("[VERIFICATION THREAD] Exception in New Player (inside of loop) processing: " + ex.Message);
							ServerConsole.AddLog(ex.StackTrace);
						}
					}
					text = JsonSerialize.ToJson(new AuthenticatorPlayerObjects(list));
					ListPool<AuthenticatorPlayerObject>.Shared.Return(list);
				}
				catch (Exception ex2)
				{
					ServerConsole.AddLog("[VERIFICATION THREAD] Exception in New Players processing: " + ex2.Message);
					ServerConsole.AddLog(ex2.StackTrace);
				}
				object obj;
				if (!ServerConsole.Update)
				{
					obj = new List<string>
					{
						"ip=" + ServerConsole.Ip,
						"players=" + ServerConsole._playersAmount + "/" + CustomNetworkManager.slots,
						"newPlayers=" + text,
						"port=" + ServerConsole.PortToReport,
						"version=2"
					};
				}
				else
				{
					obj = new List<string>
					{
						"ip=" + ServerConsole.Ip,
						"players=" + ServerConsole._playersAmount + "/" + CustomNetworkManager.slots,
						"playersList=" + ServerConsole._verificationPlayersList,
						"newPlayers=" + text,
						"port=" + ServerConsole.PortToReport,
						"pastebin=" + ConfigFile.ServerConfig.GetString("serverinfo_pastebin_id", "7wV681fT"),
						"gameVersion=" + GameCore.Version.VersionString,
						"version=2",
						"update=1",
						"info=" + StringUtils.Base64Encode(this.RefreshServerNameSafe()).Replace('+', '-'),
						"privateBeta=" + GameCore.Version.PrivateBeta,
						"staffRA=" + ServerStatic.PermissionsHandler.StaffAccess,
						"friendlyFire=" + ServerConsole.FriendlyFire
					};
					object obj2 = obj;
					byte geoblocking = (byte)CustomLiteNetLib4MirrorTransport.Geoblocking;
					((List<string>)obj2).Add("geoblocking=" + geoblocking);
					((List<string>)obj).Add("modded=" + (CustomNetworkManager.Modded || ServerConsole.TransparentlyModdedServerConfig));
					((List<string>)obj).Add("tModded=" + ServerConsole.TransparentlyModdedServerConfig);
					((List<string>)obj).Add("whitelist=" + ServerConsole.WhiteListEnabled);
					((List<string>)obj).Add("accessRestriction=" + ServerConsole.AccessRestriction);
					((List<string>)obj).Add("emailSet=" + ServerConsole._emailSet);
					((List<string>)obj).Add("enforceSameIp=" + ServerConsole.EnforceSameIp);
				}
				List<string> list2 = (List<string>)obj;
				if (!string.IsNullOrEmpty(ServerConsole.Password))
				{
					list2.Add("passcode=" + ServerConsole.Password);
				}
				ServerConsole.Update = false;
				if (!AuthenticatorQuery.SendData(list2) && !ServerConsole._printedNotVerifiedMessage)
				{
					ServerConsole._printedNotVerifiedMessage = true;
					ServerConsole.AddLog("Your server won't be visible on the public server list - (" + ServerConsole.Ip + ")", ConsoleColor.Red);
					if (!ServerConsole._emailSet)
					{
						ServerConsole.AddLog("If you are 100% sure that the server is working, can be accessed from the Internet and YOU WANT TO MAKE IT PUBLIC, please set up your email in configuration file (\"contact_email\" value) and restart the server.", ConsoleColor.Red);
					}
					else
					{
						ServerConsole.AddLog("If you want to make your server PUBLIC, please make sure that:", ConsoleColor.Red);
						ServerConsole.AddLog("- Your server is working;", ConsoleColor.Red);
						ServerConsole.AddLog("- It can be accessed from the internet without the use of a VPN/proxy;", ConsoleColor.Red);
						ServerConsole.AddLog("- It complies with the VSR (PLEASE READ: https://scpslgame.com/Verified_server_rules.pdf).", ConsoleColor.Red);
						ServerConsole.AddLog("If you have checked the above, you can use the following command:", ConsoleColor.Red);
						ServerConsole.AddLog("!verify static", ConsoleColor.Red);
						ServerConsole.AddLog("If you know that you have a dynamic IP, please use this command instead:", ConsoleColor.Red);
						ServerConsole.AddLog("!verify dynamic", ConsoleColor.Red);
					}
				}
				else
				{
					ServerConsole._printedNotVerifiedMessage = true;
				}
			}
			if (b >= 15)
			{
				b = 0;
			}
			Thread.Sleep(5000);
			if (ServerConsole.ScheduleTokenRefresh || b == 0)
			{
				ServerConsole.RefreshToken();
			}
		}
	}

	private static void PrintOnOutputs(string text, ConsoleColor color)
	{
		try
		{
			if (ServerConsole.ConsoleOutputs == null)
			{
				return;
			}
			foreach (KeyValuePair<string, IOutput> consoleOutput in ServerConsole.ConsoleOutputs)
			{
				IOutput value;
				try
				{
					if (consoleOutput.Value == null || !consoleOutput.Value.Available())
					{
						ServerConsole.ConsoleOutputs.TryRemove(consoleOutput.Key, out value);
					}
					else
					{
						consoleOutput.Value.Print(text, color);
					}
				}
				catch
				{
					ServerConsole.ConsoleOutputs.TryRemove(consoleOutput.Key, out value);
				}
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Failed to print to outputs: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Red, hideFromOutputs: true);
		}
	}

	private static void RefreshToken(bool init = false)
	{
		ServerConsole.ScheduleTokenRefresh = false;
		string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/verkey.txt";
		if (!File.Exists(path))
		{
			return;
		}
		using StreamReader streamReader = new StreamReader(path);
		string text = streamReader.ReadToEnd().Trim();
		if (!init && string.IsNullOrEmpty(ServerConsole.Password) && !string.IsNullOrEmpty(text))
		{
			ServerConsole.AddLog("Verification token loaded! Server probably will be listed on public list.");
		}
		if (ServerConsole.Password != text)
		{
			ServerConsole.AddLog("Verification token reloaded.");
			ServerConsole.Update = true;
		}
		ServerConsole.Password = text;
		CustomNetworkManager.IsVerified = true;
	}
}
