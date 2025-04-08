using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
	public static ServerConsole Singleton { get; private set; }

	public InterpolatedCommandFormatter NameFormatter { get; private set; }

	public static void ReloadServerName()
	{
		ServerConsole.ServerName = ConfigFile.ServerConfig.GetString("server_name", "My Server Name");
	}

	public void Dispose()
	{
		ServerConsole._disposing = true;
		IServerOutput serverOutput = ServerStatic.ServerOutput;
		if (serverOutput != null)
		{
			serverOutput.Dispose();
		}
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
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return;
		}
		if (ServerConsole.GetUserId() != 0U)
		{
			return;
		}
		global::GameCore.Console.AddLog("Running the game as ROOT is NOT recommended, please create a separate user!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
	}

	private void Start()
	{
		ServerConsole.CheckRoot();
		InterpolatedCommandFormatter interpolatedCommandFormatter = new InterpolatedCommandFormatter(4);
		interpolatedCommandFormatter.StartClosure = '{';
		interpolatedCommandFormatter.EndClosure = '}';
		interpolatedCommandFormatter.Escape = '\\';
		interpolatedCommandFormatter.ArgumentSplitter = ',';
		Dictionary<string, Func<List<string>, string>> dictionary = new Dictionary<string, Func<List<string>, string>>();
		dictionary.Add("ip", (List<string> args) => ServerConsole.Ip);
		dictionary.Add("port", (List<string> args) => LiteNetLib4MirrorTransport.Singleton.port.ToString());
		dictionary.Add("number", (List<string> args) => ((int)(LiteNetLib4MirrorTransport.Singleton.port - 7776)).ToString());
		dictionary.Add("version", (List<string> args) => global::GameCore.Version.VersionString);
		dictionary.Add("player_count", (List<string> args) => ReferenceHub.GetPlayerCount(new ClientInstanceMode[]
		{
			ClientInstanceMode.ReadyClient,
			ClientInstanceMode.Host
		}).ToString());
		dictionary.Add("full_player_count", delegate(List<string> args)
		{
			int playerCount = ReferenceHub.GetPlayerCount(new ClientInstanceMode[]
			{
				ClientInstanceMode.ReadyClient,
				ClientInstanceMode.Host
			});
			if (playerCount != CustomNetworkManager.TypedSingleton.ReservedMaxPlayers)
			{
				return string.Format("{0}/{1}", playerCount, CustomNetworkManager.TypedSingleton.ReservedMaxPlayers);
			}
			int count = args.Count;
			if (count == 1)
			{
				return "FULL";
			}
			if (count != 2)
			{
				throw new ArgumentOutOfRangeException("args", args, "Invalid arguments. Use: full_player_count OR full_player_count,[full]");
			}
			return this.NameFormatter.ProcessExpression(args[1]);
		});
		dictionary.Add("max_players", (List<string> args) => CustomNetworkManager.TypedSingleton.ReservedMaxPlayers.ToString());
		dictionary.Add("round_duration_minutes", (List<string> args) => RoundStart.RoundLength.Minutes.ToString("00"));
		dictionary.Add("round_duration_seconds", (List<string> args) => RoundStart.RoundLength.Seconds.ToString("00"));
		dictionary.Add("kills", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.Kills.ToString(), true));
		dictionary.Add("alive_role", delegate(List<string> args)
		{
			if (args.Count != 2)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use: alive_role,[role ID]");
			}
			RoleTypeId role;
			if (!Enum.TryParse<RoleTypeId>(this.NameFormatter.ProcessExpression(args[1]), out role))
			{
				throw new CommandInputException("role ID", args[1], "Could not parse.");
			}
			return ServerConsole.GetRoundInfo((RoundSummary s) => s.CountRole(role).ToString(), true);
		});
		dictionary.Add("alive_team", delegate(List<string> args)
		{
			if (args.Count != 2)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use: alive_team,[team ID]");
			}
			Team team;
			if (!Enum.TryParse<Team>(this.NameFormatter.ProcessExpression(args[1]), out team))
			{
				throw new CommandInputException("team ID", args[1], "Could not parse.");
			}
			return ServerConsole.GetRoundInfo((RoundSummary s) => s.CountTeam(team).ToString(), true);
		});
		dictionary.Add("zombies_recalled", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.ChangedIntoZombies.ToString(), true));
		dictionary.Add("scp_counter", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => string.Format("{0}/{1}", summary.CountTeam(Team.SCPs) - summary.CountRole(RoleTypeId.Scp0492), summary.classlistStart.scps_except_zombies), false));
		dictionary.Add("scp_start", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.scps_except_zombies.ToString(), true));
		dictionary.Add("scp_killed", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => (summary.classlistStart.scps_except_zombies - summary.CountTeam(Team.SCPs) - summary.CountRole(RoleTypeId.Scp0492)).ToString(), true));
		dictionary.Add("scp_kills", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.KilledBySCPs.ToString(), true));
		dictionary.Add("classd_counter", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => string.Format("{0}/{1}", RoundSummary.EscapedClassD, summary.classlistStart.class_ds), true));
		dictionary.Add("classd_start", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.class_ds.ToString(), true));
		dictionary.Add("classd_escaped", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.EscapedClassD.ToString(), true));
		dictionary.Add("scientist_counter", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => string.Format("{0}/{1}", RoundSummary.EscapedScientists, summary.classlistStart.scientists), false));
		dictionary.Add("scientist_start", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => summary.classlistStart.scientists.ToString(), true));
		dictionary.Add("scientist_escaped", (List<string> args) => ServerConsole.GetRoundInfo((RoundSummary summary) => RoundSummary.EscapedScientists.ToString(), true));
		dictionary.Add("mtf_respawns", (List<string> args) => ServerConsole.GetRoundInfo(delegate(RoundSummary summary)
		{
			List<string> list;
			return (NamingRulesManager.GeneratedNames.TryGetValue(Team.FoundationForces, out list) ? (list.Count - 1) : 0).ToString();
		}, true));
		dictionary.Add("warhead_detonated", delegate(List<string> args)
		{
			int count2 = args.Count;
			if (count2 == 1)
			{
				return ServerConsole.GetRoundInfo(delegate(RoundSummary s)
				{
					if (!AlphaWarheadController.Detonated)
					{
						return string.Empty;
					}
					return "☢ WARHEAD DETONATED ☢";
				}, false);
			}
			if (count2 != 3)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use: warhead_detonated OR warhead_detonated,[detonated],[undetonated]");
			}
			return ServerConsole.GetRoundInfo((RoundSummary s) => this.NameFormatter.ProcessExpression(args[AlphaWarheadController.Detonated ? 1 : 2]), false);
		});
		dictionary.Add("random", delegate(List<string> args)
		{
			int count3 = args.Count;
			float num;
			float num2;
			if (count3 != 2)
			{
				if (count3 != 3)
				{
					throw new CommandInputException("args", args, "Invalid arguments. Use: random,[max] OR random,[min],[max]");
				}
				string text = this.NameFormatter.ProcessExpression(args[1]);
				if (!float.TryParse(text, out num))
				{
					throw new CommandInputException("min", text, "Could not parse.");
				}
				string text2 = this.NameFormatter.ProcessExpression(args[2]);
				if (!float.TryParse(text2, out num2))
				{
					throw new CommandInputException("max", text2, "Could not parse.");
				}
			}
			else
			{
				num = 0f;
				string text2 = this.NameFormatter.ProcessExpression(args[1]);
				if (!float.TryParse(text2, out num2))
				{
					throw new CommandInputException("max", text2, "Could not parse.");
				}
			}
			return global::UnityEngine.Random.Range(num, num2).ToString();
		});
		dictionary.Add("random_list", delegate(List<string> args)
		{
			if (args.Count < 3)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use: random_list,[entry 1],[entry 2]...");
			}
			return this.NameFormatter.ProcessExpression(args[global::UnityEngine.Random.Range(1, args.Count)]);
		});
		dictionary.Add("constant_e", (List<string> args) => 2.7182817f.ToString());
		dictionary.Add("constant_pi", (List<string> args) => 3.1415927f.ToString());
		dictionary.Add("add", (List<string> args) => this.StandardizedFloatComparison<float>("add", args, (float a, float b) => a + b));
		dictionary.Add("subtract", (List<string> args) => this.StandardizedFloatComparison<float>("subtract", args, (float a, float b) => a - b));
		dictionary.Add("multiply", (List<string> args) => this.StandardizedFloatComparison<float>("multiply", args, (float a, float b) => a * b));
		dictionary.Add("division", (List<string> args) => this.StandardizedFloatComparison<float>("division", args, (float a, float b) => a / b));
		dictionary.Add("power", (List<string> args) => this.StandardizedFloatComparison<float>("power", args, ServerConsole._pow));
		dictionary.Add("log", delegate(List<string> args)
		{
			int count4 = args.Count;
			float num3;
			float num4;
			if (count4 != 2)
			{
				if (count4 != 3)
				{
					throw new CommandInputException("args", args, "Invalid arguments. Use log,[value] OR log,[value],[base]");
				}
				string text3 = this.NameFormatter.ProcessExpression(args[1]);
				if (!float.TryParse(text3, out num3))
				{
					throw new CommandInputException("value", text3, "Could not parse.");
				}
				string text4 = this.NameFormatter.ProcessExpression(args[2]);
				if (!float.TryParse(text4, out num4))
				{
					throw new CommandInputException("base", text4, "Could not parse.");
				}
			}
			else
			{
				string text3 = this.NameFormatter.ProcessExpression(args[1]);
				if (!float.TryParse(text3, out num3))
				{
					throw new CommandInputException("value", text3, "Could not parse.");
				}
				num4 = 10f;
			}
			return Mathf.Log(num3, num4).ToString();
		});
		dictionary.Add("ln", delegate(List<string> args)
		{
			if (args.Count < 2)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use ln,[value]");
			}
			string text5 = this.NameFormatter.ProcessExpression(args[1]);
			float num5;
			if (!float.TryParse(text5, out num5))
			{
				throw new CommandInputException("value", text5, "Error parsing value.");
			}
			return Mathf.Log(num5).ToString();
		});
		dictionary.Add("round", (List<string> args) => this.StandardizedFloatRound("round", args, ServerConsole._roundNormal));
		dictionary.Add("round_up", (List<string> args) => this.StandardizedFloatRound("round_up", args, ServerConsole._roundCeil));
		dictionary.Add("round_down", (List<string> args) => this.StandardizedFloatRound("round_down", args, ServerConsole._roundFloor));
		dictionary.Add("equals", delegate(List<string> args)
		{
			if (args.Count != 3)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use equals,[object A],[object B]");
			}
			return (args[1] == args[2]).ToString();
		});
		dictionary.Add("greater", (List<string> args) => this.StandardizedFloatComparison<bool>("greater", args, (float a, float b) => a > b));
		dictionary.Add("lesser", (List<string> args) => this.StandardizedFloatComparison<bool>("lesser", args, (float a, float b) => a < b));
		dictionary.Add("greater_or_equal", (List<string> args) => this.StandardizedFloatComparison<bool>("greater_or_equal", args, (float a, float b) => a >= b));
		dictionary.Add("lesser_or_equal", (List<string> args) => this.StandardizedFloatComparison<bool>("lesser_or_equal", args, (float a, float b) => a <= b));
		dictionary.Add("not", delegate(List<string> args)
		{
			if (args.Count != 2)
			{
				throw new CommandInputException("args", args, "Invalid arguments. Use not,[value]");
			}
			string text6 = this.NameFormatter.ProcessExpression(args[1]);
			bool flag;
			if (!bool.TryParse(text6, out flag))
			{
				throw new CommandInputException("value", text6, "Error parsing value.");
			}
			return (!flag).ToString();
		});
		dictionary.Add("or", (List<string> args) => this.StandardizedBoolComparison<bool>("or", args, (bool a, bool b) => a || b));
		dictionary.Add("xor", (List<string> args) => this.StandardizedBoolComparison<bool>("xor", args, (bool a, bool b) => a ^ b));
		dictionary.Add("and", (List<string> args) => this.StandardizedBoolComparison<bool>("and", args, (bool a, bool b) => a && b));
		dictionary.Add("if", delegate(List<string> args)
		{
			int count5 = args.Count;
			string text7;
			string text8;
			if (count5 != 3)
			{
				if (count5 != 4)
				{
					throw new CommandInputException("args", args, "Invalid arguments. Use if,[condition],[action] OR if,[condition],[action],[else action]");
				}
				text7 = args[2];
				text8 = args[3];
			}
			else
			{
				text7 = args[2];
				text8 = string.Empty;
			}
			string text9 = this.NameFormatter.ProcessExpression(args[1]);
			bool flag2;
			if (!bool.TryParse(text9, out flag2))
			{
				throw new CommandInputException("condition", text9, "Could not parse.");
			}
			return this.NameFormatter.ProcessExpression(flag2 ? text7 : text8);
		});
		interpolatedCommandFormatter.Commands = dictionary;
		this.NameFormatter = interpolatedCommandFormatter;
		PlayerAuthenticationManager.OnInstanceModeChanged += ServerConsole.HandlePlayerJoin;
		ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub rh)
		{
			ServerConsole.RefreshOnlinePlayers();
		}));
		if (!ServerStatic.IsDedicated)
		{
			return;
		}
		if (!ServerStatic.ProcessIdPassed)
		{
			return;
		}
		ServerConsole._checkProcessThread = new Thread(new ThreadStart(ServerConsole.CheckProcess))
		{
			Priority = global::System.Threading.ThreadPriority.Lowest,
			IsBackground = true,
			Name = "Dedicated server console running check"
		};
		ServerConsole._checkProcessThread.Start();
	}

	private static void HandlePlayerJoin(ReferenceHub rh, ClientInstanceMode mode)
	{
		if (mode != ClientInstanceMode.ReadyClient)
		{
			return;
		}
		ServerConsole.NewPlayers.Add(rh);
		ServerConsole.RefreshOnlinePlayers();
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
		string text;
		while (ServerConsole.PrompterQueue.TryDequeue(out text))
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				ServerConsole.EnterCommand(text, ServerConsole.Scs);
			}
		}
	}

	private static void RefreshOnlinePlayers()
	{
		try
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				ClientInstanceMode mode = referenceHub.Mode;
				if ((mode == ClientInstanceMode.ReadyClient || mode == ClientInstanceMode.Host) && !string.IsNullOrEmpty(referenceHub.authManager.UserId) && (!referenceHub.isLocalPlayer || !ServerStatic.IsDedicated))
				{
					ServerConsole.PlayersListRaw.objects.Add(referenceHub.authManager.UserId);
				}
			}
			ServerConsole._verificationPlayersList = JsonSerialize.ToJson<PlayerListSerialized>(ServerConsole.PlayersListRaw);
			ServerConsole._playersAmount = ServerConsole.PlayersListRaw.objects.Count;
			SteamServerInfo.OnlinePlayers = ServerConsole._playersAmount;
			ServerConsole.PlayersListRaw.objects.Clear();
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("[VERIFICATION] Exception in Players Online processing: " + ex.Message, ConsoleColor.Gray, false);
			ServerConsole.AddLog(ex.StackTrace, ConsoleColor.Gray, false);
		}
	}

	private string StandardizedBoolComparison<T>(string source, IReadOnlyList<string> args, Func<bool, bool, T> comparison)
	{
		return this.StandardizedComparison<bool, T>(source, args, delegate(string arg)
		{
			bool flag;
			return new ValueTuple<bool, bool>(bool.TryParse(arg, out flag), flag);
		}, comparison);
	}

	private string StandardizedFloatComparison<T>(string source, IReadOnlyList<string> args, Func<float, float, T> comparison)
	{
		return this.StandardizedComparison<float, T>(source, args, delegate(string arg)
		{
			float num;
			return new ValueTuple<bool, float>(float.TryParse(arg, out num), num);
		}, comparison);
	}

	private string StandardizedComparison<TArg, TResult>(string source, IReadOnlyList<string> args, [TupleElementNames(new string[] { "success", "value" })] Func<string, ValueTuple<bool, TArg>> parse, Func<TArg, TArg, TResult> comparison)
	{
		if (args.Count != 3)
		{
			throw new CommandInputException("args", args, "Invalid arguments. Use " + source + ",[value A],[value B]");
		}
		string text = this.NameFormatter.ProcessExpression(args[1]);
		ValueTuple<bool, TArg> valueTuple = parse(text);
		bool item = valueTuple.Item1;
		TArg item2 = valueTuple.Item2;
		if (!item)
		{
			throw new CommandInputException("value A", args[1], "Could not parse.");
		}
		string text2 = this.NameFormatter.ProcessExpression(args[2]);
		ValueTuple<bool, TArg> valueTuple2 = parse(text2);
		bool item3 = valueTuple2.Item1;
		TArg item4 = valueTuple2.Item2;
		if (!item3)
		{
			throw new CommandInputException("value B", text2, "Could not parse.");
		}
		TResult tresult = comparison(item2, item4);
		return tresult.ToString();
	}

	private string StandardizedFloatRound(string source, IReadOnlyList<string> args, Func<float, float> rounder)
	{
		int count = args.Count;
		float num;
		int num2;
		if (count != 2)
		{
			if (count != 3)
			{
				throw new CommandInputException("args", args, string.Concat(new string[] { "Invalid arguments. Use ", source, ",[value] OR ", source, ",[value],[precision]" }));
			}
			string text = this.NameFormatter.ProcessExpression(args[1]);
			if (!float.TryParse(text, out num))
			{
				throw new CommandInputException("value", text, "Could not parse.");
			}
			string text2 = this.NameFormatter.ProcessExpression(args[1]);
			if (!int.TryParse(text2, out num2))
			{
				throw new CommandInputException("precision", text2, "Could not parse.");
			}
		}
		else
		{
			string text = this.NameFormatter.ProcessExpression(args[1]);
			if (!float.TryParse(text, out num))
			{
				throw new CommandInputException("value", text, "Could not parse.");
			}
			num2 = 0;
		}
		float num3 = Mathf.Pow(10f, (float)num2);
		return (rounder(num * num3) / num3).ToString(CultureInfo.InvariantCulture);
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
		string text;
		if (this.NameFormatter.TryProcessExpression(ServerConsole.ServerName, "server name", out text))
		{
			SteamServerInfo.ServerName = Regex.Replace(text, "<[^>]*>", string.Empty);
			return text;
		}
		ServerConsole.AddLog(text, ConsoleColor.Gray, false);
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
				Process consoleProcess = ServerConsole.ConsoleProcess;
				if (consoleProcess != null)
				{
					consoleProcess.Dispose();
				}
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
		IServerOutput serverOutput = ServerStatic.ServerOutput;
		if (serverOutput != null)
		{
			serverOutput.AddOutput(entry);
		}
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
		if (component == null)
		{
			return;
		}
		CharacterClassManager component2 = player.GetComponent<CharacterClassManager>();
		if (component2 == null)
		{
			component.connectionToClient.Disconnect();
			return;
		}
		component2.DisconnectClient(component.connectionToClient, message);
	}

	public static void Disconnect(NetworkConnection conn, string message)
	{
		GameObject gameObject = global::GameCore.Console.FindConnectedRoot(conn);
		if (gameObject == null)
		{
			conn.Disconnect();
			return;
		}
		ServerConsole.Disconnect(gameObject, message);
	}

	public static string ColorText(string text, ConsoleColor color)
	{
		return string.Concat(new string[]
		{
			"<color=",
			ServerConsole.ConsoleColorToHex(color),
			">",
			text,
			"</color>"
		});
	}

	public static void ColorDebugLog(string text, ConsoleColor color)
	{
		global::UnityEngine.Debug.Log(ServerConsole.ColorText(text, color), null);
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
				IServerOutput serverOutput = ServerStatic.ServerOutput;
				if (serverOutput != null)
				{
					serverOutput.AddLog(text2, defaultColor);
				}
			}
			else
			{
				string text3 = text2.Substring(7);
				text3 = text3.Substring(0, text3.IndexOf('>')).Replace("\"", "").Replace("'", "");
				string text4 = text2.Substring(text2.IndexOf('>') + 1);
				text4 = text4.Substring(0, text4.IndexOf('<'));
				bool flag = text3.StartsWith("#", StringComparison.Ordinal);
				ConsoleColor consoleColor = defaultColor;
				Color32 color;
				ConsoleColor consoleColor2;
				if (flag && Misc.TryParseColor(text3, out color))
				{
					consoleColor = Misc.ClosestConsoleColor(color, false);
				}
				else if (!flag && Enum.TryParse<ConsoleColor>(text3, true, out consoleColor2))
				{
					consoleColor = consoleColor2;
				}
				IServerOutput serverOutput2 = ServerStatic.ServerOutput;
				if (serverOutput2 != null)
				{
					serverOutput2.AddLog(text4, consoleColor);
				}
				i++;
			}
		}
	}

	public static string ConsoleColorToHex(ConsoleColor color)
	{
		switch (color)
		{
		case ConsoleColor.Black:
			return "#000000";
		case ConsoleColor.DarkBlue:
			return "#0058F8";
		case ConsoleColor.DarkGreen:
			return "#005800";
		case ConsoleColor.DarkCyan:
			return "#00B7EB";
		case ConsoleColor.DarkRed:
			return "#A80020";
		case ConsoleColor.DarkMagenta:
			return "#FF0090";
		case ConsoleColor.DarkYellow:
			return "#AC7C00";
		case ConsoleColor.Gray:
			return "#D8D8D8";
		case ConsoleColor.DarkGray:
			return "#787878";
		case ConsoleColor.Blue:
			return "#0078F8";
		case ConsoleColor.Green:
			return "#00B800";
		case ConsoleColor.Cyan:
			return "#00FFFF";
		case ConsoleColor.Red:
			return "#DC143C";
		case ConsoleColor.Magenta:
			return "#FF00FF";
		case ConsoleColor.Yellow:
			return "#F8B800";
		case ConsoleColor.White:
			return "#FCFCFC";
		default:
			return "#6844FC";
		}
	}

	public static Color ConsoleColorToColor(ConsoleColor color)
	{
		switch (color)
		{
		case ConsoleColor.Black:
			return Color.black;
		case ConsoleColor.DarkBlue:
			return new Color(0f, 88f, 248f);
		case ConsoleColor.DarkGreen:
			return new Color(0f, 88f, 0f);
		case ConsoleColor.DarkCyan:
			return new Color(0f, 183f, 235f);
		case ConsoleColor.DarkRed:
			return new Color(168f, 0f, 32f);
		case ConsoleColor.DarkMagenta:
			return new Color(255f, 0f, 144f);
		case ConsoleColor.DarkYellow:
			return new Color(172f, 124f, 0f);
		case ConsoleColor.Gray:
			return new Color(216f, 216f, 216f);
		case ConsoleColor.DarkGray:
			return Color.gray;
		case ConsoleColor.Blue:
			return Color.blue;
		case ConsoleColor.Green:
			return Color.green;
		case ConsoleColor.Cyan:
			return Color.cyan;
		case ConsoleColor.Red:
			return Color.red;
		case ConsoleColor.Magenta:
			return Color.magenta;
		case ConsoleColor.Yellow:
			return Color.yellow;
		case ConsoleColor.White:
			return new Color(252f, 252f, 252f);
		default:
			return new Color(104f, 68f, 252f);
		}
	}

	public static string EnterCommand(string cmds, CommandSender sender = null)
	{
		string[] args = cmds.Split(' ', StringSplitOptions.None);
		if (args.Length == 0)
		{
			return string.Empty;
		}
		if (sender == null)
		{
			sender = ServerConsole.Scs;
		}
		string cmd = args[0];
		if (!cmd.StartsWith("!", StringComparison.Ordinal) || cmd.Length <= 1)
		{
			return global::GameCore.Console.singleton.TypeCommand(cmds, sender ?? ServerConsole.Scs);
		}
		if (cmd.StartsWith("!verify", StringComparison.OrdinalIgnoreCase) && !ServerConsole._emailSet)
		{
			return "You have to set the contact email address (\"contact_email\" key in the gameplay config) before running this command!";
		}
		new Thread(delegate
		{
			string text = cmd.Substring(1).ToLower();
			string text2;
			if (args.Length != 1)
			{
				text2 = args.Skip(1).Aggregate((string current, string next) => current + " " + next);
			}
			else
			{
				text2 = "";
			}
			ServerConsole.RunCentralServerCommand(text, text2);
		})
		{
			IsBackground = true,
			Priority = global::System.Threading.ThreadPriority.AboveNormal,
			Name = "SCP:SL Central server command execution"
		}.Start();
		return "Sending command to central servers...";
	}

	public void RunServer()
	{
		Thread verificationRequestThread = ServerConsole._verificationRequestThread;
		if (verificationRequestThread != null && verificationRequestThread.IsAlive)
		{
			return;
		}
		ServerConsole._verificationRequestThread = new Thread(new ThreadStart(this.RefreshServerData))
		{
			IsBackground = true,
			Priority = global::System.Threading.ThreadPriority.AboveNormal,
			Name = "SCP:SL Server list thread"
		};
		ServerConsole._verificationRequestThread.Start();
	}

	internal static void RunRefreshPublicKey()
	{
		Thread refreshPublicKeyThread = ServerConsole._refreshPublicKeyThread;
		if (refreshPublicKeyThread != null && refreshPublicKeyThread.IsAlive)
		{
			return;
		}
		ServerConsole._refreshPublicKeyThread = new Thread(new ThreadStart(ServerConsole.RefreshPublicKey))
		{
			IsBackground = true,
			Priority = global::System.Threading.ThreadPriority.Normal,
			Name = "SCP:SL Public key refreshing"
		};
		ServerConsole._refreshPublicKeyThread.Start();
	}

	internal static void RunRefreshPublicKeyOnce()
	{
		Thread refreshPublicKeyOnceThread = ServerConsole._refreshPublicKeyOnceThread;
		if (refreshPublicKeyOnceThread != null && refreshPublicKeyOnceThread.IsAlive)
		{
			return;
		}
		ServerConsole._refreshPublicKeyOnceThread = new Thread(new ThreadStart(ServerConsole.RefreshPublicKeyOnce))
		{
			IsBackground = true,
			Priority = global::System.Threading.ThreadPriority.AboveNormal,
			Name = "SCP:SL Public key refreshing ON DEMAND"
		};
		ServerConsole._refreshPublicKeyOnceThread.Start();
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
			ServerConsole.AddLog("Loaded central server public key from cache.", ConsoleColor.Gray, false);
			ServerConsole.AddLog("SHA256 of public key: " + text2, ConsoleColor.Gray, false);
		}
		ServerConsole.AddLog("Downloading public key from central server...", ConsoleColor.Gray, false);
		while (!ServerConsole._disposing)
		{
			try
			{
				PublicKeyResponse publicKeyResponse = JsonSerialize.FromJson<PublicKeyResponse>(HttpQuery.Get(string.Format("{0}v5/publickey.php?major={1}", CentralServer.StandardUrl, global::GameCore.Version.Major)));
				if (!ECDSA.Verify(publicKeyResponse.key, publicKeyResponse.signature, CentralServerKeyCache.MasterKey))
				{
					global::GameCore.Console.AddLog("Can't refresh central server public key - invalid signature!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
					Thread.Sleep(360000);
					continue;
				}
				ServerConsole.PublicKey = ECDSA.PublicKeyFromString(publicKeyResponse.key);
				string text4 = Sha.HashToString(Sha.Sha256(ECDSA.KeyToString(ServerConsole.PublicKey)));
				if (text4 != text3)
				{
					text3 = text4;
					ServerConsole.AddLog("Downloaded public key from central server.", ConsoleColor.Gray, false);
					ServerConsole.AddLog("SHA256 of public key: " + text4, ConsoleColor.Gray, false);
					if (text4 != text2)
					{
						CentralServerKeyCache.SaveCache(publicKeyResponse.key, publicKeyResponse.signature);
					}
					else
					{
						ServerConsole.AddLog("SHA256 of cached key matches, no need to update cache.", ConsoleColor.Gray, false);
					}
				}
				else if (flag)
				{
					flag = false;
					ServerConsole.AddLog("Refreshed public key of central server - key hash not changed.", ConsoleColor.Gray, false);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("Can't refresh central server public key - " + ex.Message, ConsoleColor.Gray, false);
			}
			Thread.Sleep(360000);
		}
	}

	private static void RefreshPublicKeyOnce()
	{
		try
		{
			PublicKeyResponse publicKeyResponse = JsonSerialize.FromJson<PublicKeyResponse>(HttpQuery.Get(string.Format("{0}v5/publickey.php?major={1}", CentralServer.StandardUrl, global::GameCore.Version.Major)));
			if (!ECDSA.Verify(publicKeyResponse.key, publicKeyResponse.signature, CentralServerKeyCache.MasterKey))
			{
				global::GameCore.Console.AddLog("Can't refresh central server public key - invalid signature!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			}
			else
			{
				ServerConsole.PublicKey = ECDSA.PublicKeyFromString(publicKeyResponse.key);
				string text = Sha.HashToString(Sha.Sha256(ECDSA.KeyToString(ServerConsole.PublicKey)));
				ServerConsole.AddLog("Downloaded public key from central server.", ConsoleColor.Gray, false);
				ServerConsole.AddLog("SHA256 of public key: " + text, ConsoleColor.Gray, false);
				CentralServerKeyCache.SaveCache(publicKeyResponse.key, publicKeyResponse.signature);
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Can't refresh central server public key - " + ex.Message, ConsoleColor.Gray, false);
		}
	}

	private static void RunCentralServerCommand(string cmd, string args)
	{
		cmd = cmd.ToLower();
		List<string> list = new List<string>
		{
			"ip=" + ServerConsole.Ip,
			"port=" + ServerConsole.PortToReport.ToString(),
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
			ServerConsole.AddLog("[" + cmd + "] " + text, ConsoleColor.Gray, false);
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Could not execute the central server command \"" + cmd + "\" - (LOCAL EXCEPTION) " + ex.Message, ConsoleColor.Red, false);
		}
	}

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

	internal static void RefreshEmailSetStatus()
	{
		ServerConsole._emailSet = !string.IsNullOrEmpty(ConfigFile.ServerConfig.GetString("contact_email", ""));
	}

	private void RefreshServerData()
	{
		bool flag = true;
		byte b = 0;
		ServerConsole.RefreshEmailSetStatus();
		ServerConsole.RefreshToken(true);
		while (!ServerConsole._disposing)
		{
			b += 1;
			if (!flag && string.IsNullOrEmpty(ServerConsole.Password) && b < 15)
			{
				if (b == 5 || b == 12 || ServerConsole.ScheduleTokenRefresh)
				{
					ServerConsole.RefreshToken(false);
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
							ReferenceHub referenceHub;
							if (ServerConsole.NewPlayers.TryTake(out referenceHub) && referenceHub != null)
							{
								list.Add(new AuthenticatorPlayerObject(referenceHub.authManager.UserId, (referenceHub.authManager.connectionToClient == null || string.IsNullOrEmpty(referenceHub.authManager.connectionToClient.address)) ? "N/A" : referenceHub.authManager.connectionToClient.address, referenceHub.authManager.AuthenticationResponse.AuthToken.RequestIp, referenceHub.authManager.AuthenticationResponse.AuthToken.Asn.ToString(), referenceHub.authManager.AuthenticationResponse.AuthToken.Serial, referenceHub.authManager.AuthenticationResponse.AuthToken.VacSession));
							}
						}
						catch (Exception ex)
						{
							ServerConsole.AddLog("[VERIFICATION THREAD] Exception in New Player (inside of loop) processing: " + ex.Message, ConsoleColor.Gray, false);
							ServerConsole.AddLog(ex.StackTrace, ConsoleColor.Gray, false);
						}
					}
					text = JsonSerialize.ToJson<AuthenticatorPlayerObjects>(new AuthenticatorPlayerObjects(list));
					ListPool<AuthenticatorPlayerObject>.Shared.Return(list);
				}
				catch (Exception ex2)
				{
					ServerConsole.AddLog("[VERIFICATION THREAD] Exception in New Players processing: " + ex2.Message, ConsoleColor.Gray, false);
					ServerConsole.AddLog(ex2.StackTrace, ConsoleColor.Gray, false);
				}
				List<string> list3;
				if (!ServerConsole.Update)
				{
					List<string> list2 = new List<string>();
					list2.Add("ip=" + ServerConsole.Ip);
					list2.Add("players=" + ServerConsole._playersAmount.ToString() + "/" + CustomNetworkManager.slots.ToString());
					list2.Add("newPlayers=" + text);
					list2.Add("port=" + ServerConsole.PortToReport.ToString());
					list3 = list2;
					list2.Add("version=2");
				}
				else
				{
					List<string> list4 = new List<string>();
					list4.Add("ip=" + ServerConsole.Ip);
					list4.Add("players=" + ServerConsole._playersAmount.ToString() + "/" + CustomNetworkManager.slots.ToString());
					list4.Add("playersList=" + ServerConsole._verificationPlayersList);
					list4.Add("newPlayers=" + text);
					list4.Add("port=" + ServerConsole.PortToReport.ToString());
					list4.Add("pastebin=" + ConfigFile.ServerConfig.GetString("serverinfo_pastebin_id", "7wV681fT"));
					list4.Add("gameVersion=" + global::GameCore.Version.VersionString);
					list4.Add("version=2");
					list4.Add("update=1");
					list4.Add("info=" + StringUtils.Base64Encode(this.RefreshServerNameSafe()).Replace('+', '-'));
					list4.Add("privateBeta=" + global::GameCore.Version.PrivateBeta.ToString());
					list4.Add("staffRA=" + ServerStatic.PermissionsHandler.StaffAccess.ToString());
					list4.Add("friendlyFire=" + ServerConsole.FriendlyFire.ToString());
					string text2 = "geoblocking=";
					byte geoblocking = (byte)CustomLiteNetLib4MirrorTransport.Geoblocking;
					list4.Add(text2 + geoblocking.ToString());
					list4.Add("modded=" + (CustomNetworkManager.Modded || ServerConsole.TransparentlyModdedServerConfig).ToString());
					list4.Add("tModded=" + ServerConsole.TransparentlyModdedServerConfig.ToString());
					list4.Add("whitelist=" + ServerConsole.WhiteListEnabled.ToString());
					list4.Add("accessRestriction=" + ServerConsole.AccessRestriction.ToString());
					list4.Add("emailSet=" + ServerConsole._emailSet.ToString());
					list3 = list4;
					list4.Add("enforceSameIp=" + ServerConsole.EnforceSameIp.ToString());
				}
				List<string> list5 = list3;
				if (!string.IsNullOrEmpty(ServerConsole.Password))
				{
					list5.Add("passcode=" + ServerConsole.Password);
				}
				ServerConsole.Update = false;
				if (!AuthenticatorQuery.SendData(list5) && !ServerConsole._printedNotVerifiedMessage)
				{
					ServerConsole._printedNotVerifiedMessage = true;
					ServerConsole.AddLog("Your server won't be visible on the public server list - (" + ServerConsole.Ip + ")", ConsoleColor.Red, false);
					if (!ServerConsole._emailSet)
					{
						ServerConsole.AddLog("If you are 100% sure that the server is working, can be accessed from the Internet and YOU WANT TO MAKE IT PUBLIC, please set up your email in configuration file (\"contact_email\" value) and restart the server.", ConsoleColor.Red, false);
					}
					else
					{
						ServerConsole.AddLog("If you are 100% sure that the server is working, can be accessed from the Internet and YOU WANT TO MAKE IT PUBLIC please email following information:", ConsoleColor.Red, false);
						ServerConsole.AddLog("- IP address of server (most likely " + ServerConsole.Ip + ")", ConsoleColor.Red, false);
						ServerConsole.AddLog("- port of the server (currently the server is running on port " + ServerConsole.PortToReport.ToString() + ")", ConsoleColor.Red, false);
						ServerConsole.AddLog("- is this static or dynamic IP address (most of home adresses are dynamic)", ConsoleColor.Red, false);
						ServerConsole.AddLog("PLEASE READ rules for verified servers first: https://scpslgame.com/Verified_server_rules.pdf", ConsoleColor.Red, false);
						ServerConsole.AddLog("send us that information to: server.verification@scpslgame.com (server.verification at scpslgame.com)", ConsoleColor.Red, false);
						ServerConsole.AddLog("if you can't see the AT sign in console (in above line): server.verification AT scpslgame.com", ConsoleColor.Red, false);
						ServerConsole.AddLog("email must be sent from email address set as \"contact_email\" in your config file (current value: " + ConfigFile.ServerConfig.GetString("contact_email", "") + ").", ConsoleColor.Red, false);
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
				ServerConsole.RefreshToken(false);
			}
		}
	}

	private static void PrintOnOutputs(string text, ConsoleColor color)
	{
		try
		{
			if (ServerConsole.ConsoleOutputs != null)
			{
				foreach (KeyValuePair<string, IOutput> keyValuePair in ServerConsole.ConsoleOutputs)
				{
					try
					{
						if (keyValuePair.Value == null || !keyValuePair.Value.Available())
						{
							IOutput output;
							ServerConsole.ConsoleOutputs.TryRemove(keyValuePair.Key, out output);
						}
						else
						{
							keyValuePair.Value.Print(text, color);
						}
					}
					catch
					{
						IOutput output;
						ServerConsole.ConsoleOutputs.TryRemove(keyValuePair.Key, out output);
					}
				}
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Failed to print to outputs: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Red, true);
		}
	}

	private static void RefreshToken(bool init = false)
	{
		ServerConsole.ScheduleTokenRefresh = false;
		string text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/SCP Secret Laboratory/verkey.txt";
		if (!File.Exists(text))
		{
			return;
		}
		using (StreamReader streamReader = new StreamReader(text))
		{
			string text2 = streamReader.ReadToEnd().Trim();
			if (!init && string.IsNullOrEmpty(ServerConsole.Password) && !string.IsNullOrEmpty(text2))
			{
				ServerConsole.AddLog("Verification token loaded! Server probably will be listed on public list.", ConsoleColor.Gray, false);
			}
			if (ServerConsole.Password != text2)
			{
				ServerConsole.AddLog("Verification token reloaded.", ConsoleColor.Gray, false);
				ServerConsole.Update = true;
			}
			ServerConsole.Password = text2;
			CustomNetworkManager.IsVerified = true;
		}
	}

	public static string ServerName = string.Empty;

	private static readonly Func<float, float> _roundNormal = new Func<float, float>(Mathf.Round);

	private static readonly Func<float, float> _roundCeil = new Func<float, float>(Mathf.Ceil);

	private static readonly Func<float, float> _roundFloor = new Func<float, float>(Mathf.Floor);

	private static readonly Func<float, float, float> _pow = new Func<float, float, float>(Mathf.Pow);

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
}
