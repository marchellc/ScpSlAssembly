using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using GameCore;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

public class ServerLogs : MonoBehaviour
{
	static ServerLogs()
	{
		foreach (string text in ServerLogs.Txt)
		{
			ServerLogs._maxlen = Math.Max(ServerLogs._maxlen, text.Length);
		}
		foreach (string text2 in ServerLogs.Modulestxt)
		{
			ServerLogs._modulemaxlen = Math.Max(ServerLogs._modulemaxlen, text2.Length);
		}
	}

	internal static void StartLogging()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (ServerLogs._state != ServerLogs.LoggingState.Off)
		{
			ServerLogs._state = ServerLogs.LoggingState.Restart;
			return;
		}
		Thread appendThread = ServerLogs._appendThread;
		if (appendThread != null && appendThread.IsAlive)
		{
			return;
		}
		ServerLogs._appendThread = new Thread(new ThreadStart(ServerLogs.AppendLog))
		{
			Name = "Saving server logs to file",
			Priority = global::System.Threading.ThreadPriority.BelowNormal,
			IsBackground = true
		};
		ServerLogs._appendThread.Start();
	}

	public static void AddLog(ServerLogs.Modules module, string msg, ServerLogs.ServerLogType type, bool init = false)
	{
		string text = TimeBehaviour.Rfc3339Time();
		object lockObject = ServerLogs.LockObject;
		lock (lockObject)
		{
			ServerLogs.Queue.Enqueue(new ServerLogs.ServerLog(msg, ServerLogs.Txt[(int)type], ServerLogs.Modulestxt[(int)module], text));
		}
		if (init)
		{
			return;
		}
		ServerLogs._state = ServerLogs.LoggingState.Write;
	}

	private void OnApplicationQuit()
	{
		ServerLogs._state = ServerLogs.LoggingState.Terminate;
	}

	private static void AppendLog()
	{
		ServerLogs._state = ServerLogs.LoggingState.Standby;
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		while (ServerLogs._state != ServerLogs.LoggingState.Terminate)
		{
			object obj = ServerLogs.LockObject;
			lock (obj)
			{
				ServerLogs.Queue.Clear();
				ServerLogs._state = ServerLogs.LoggingState.Standby;
				goto IL_0070;
			}
			goto IL_0048;
			IL_0070:
			if (NetworkServer.active)
			{
				string text = TimeBehaviour.FormatTime("yyyy-MM-dd HH.mm.ss");
				string text2 = LiteNetLib4MirrorTransport.Singleton.port.ToString();
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Started logging.", ServerLogs.ServerLogType.InternalMessage, true);
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Game version: " + global::GameCore.Version.VersionString + ".", ServerLogs.ServerLogType.InternalMessage, true);
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Build type: " + global::GameCore.Version.BuildType.ToString() + ".", ServerLogs.ServerLogType.InternalMessage, true);
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Build timestamp: 2025-04-04 16:40:29Z.", ServerLogs.ServerLogType.InternalMessage, true);
				ServerLogs.Modules modules = ServerLogs.Modules.GameLogic;
				string text3 = "Headless: ";
				bool flag = PlatformInfo.singleton.IsHeadless;
				ServerLogs.AddLog(modules, text3 + flag.ToString() + ".", ServerLogs.ServerLogType.InternalMessage, true);
				while (NetworkServer.active && ServerLogs._state != ServerLogs.LoggingState.Terminate && ServerLogs._state != ServerLogs.LoggingState.Restart)
				{
					Thread.Sleep(100);
					if (ServerLogs._state != ServerLogs.LoggingState.Standby)
					{
						if (!Directory.Exists(FileManager.GetAppFolder(true, false, "")))
						{
							return;
						}
						if (!Directory.Exists(FileManager.GetAppFolder(true, false, "") + "ServerLogs"))
						{
							Directory.CreateDirectory(FileManager.GetAppFolder(true, false, "") + "ServerLogs");
						}
						if (!Directory.Exists(FileManager.GetAppFolder(true, false, "") + "ServerLogs/" + text2))
						{
							Directory.CreateDirectory(FileManager.GetAppFolder(true, false, "") + "ServerLogs/" + text2);
						}
						obj = ServerLogs.LockObject;
						lock (obj)
						{
							ServerLogs.ServerLog serverLog;
							while (ServerLogs.Queue.TryDequeue(out serverLog))
							{
								string text4 = string.Concat(new string[]
								{
									serverLog.Time,
									" | ",
									ServerLogs.ToMax(serverLog.Type, ServerLogs._maxlen),
									" | ",
									ServerLogs.ToMax(serverLog.Module, ServerLogs._modulemaxlen),
									" | ",
									serverLog.Content
								});
								stringBuilder.AppendLine(text4);
								ServerLogs.PrintOnOutputs(text4);
							}
						}
						using (StreamWriter streamWriter = new StreamWriter(string.Concat(new string[]
						{
							FileManager.GetAppFolder(true, false, ""),
							"ServerLogs/",
							text2,
							"/Round ",
							text,
							".txt"
						}), true))
						{
							streamWriter.Write(stringBuilder.ToString());
						}
						stringBuilder.Clear();
						ServerLogs.LoggingState state = ServerLogs._state;
						if (state == ServerLogs.LoggingState.Terminate || state == ServerLogs.LoggingState.Restart)
						{
							break;
						}
						ServerLogs._state = ServerLogs.LoggingState.Standby;
					}
				}
				continue;
			}
			IL_0048:
			if (ServerLogs._state == ServerLogs.LoggingState.Terminate)
			{
				ServerLogs._state = ServerLogs.LoggingState.Off;
				StringBuilderPool.Shared.Return(stringBuilder);
				return;
			}
			Thread.Sleep(200);
			goto IL_0070;
		}
		ServerLogs._state = ServerLogs.LoggingState.Off;
		StringBuilderPool.Shared.Return(stringBuilder);
	}

	private static void PrintOnOutputs(string text)
	{
		try
		{
			if (ServerLogs.LiveLogOutput != null)
			{
				foreach (KeyValuePair<string, IOutput> keyValuePair in ServerLogs.LiveLogOutput)
				{
					try
					{
						if (keyValuePair.Value == null || !keyValuePair.Value.Available())
						{
							IOutput output;
							ServerLogs.LiveLogOutput.TryRemove(keyValuePair.Key, out output);
						}
						else if (keyValuePair.Value is ServerConsoleSender)
						{
							ServerConsole.AddLog(text, ConsoleColor.Gray, true);
						}
						else
						{
							keyValuePair.Value.Print(text);
						}
					}
					catch
					{
						IOutput output;
						ServerLogs.LiveLogOutput.TryRemove(keyValuePair.Key, out output);
					}
				}
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Failed to print log to outputs: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Red, false);
		}
	}

	private static string ToMax(string text, int max)
	{
		while (text.Length < max)
		{
			text += " ";
		}
		return text;
	}

	private static readonly string[] Txt = new string[]
	{
		"Connection update", "Remote Admin", "Remote Admin - Misc", "Kill", "Game Event", "Internal", "Auth Rate Limit", "Teamkill", "Suicide", "AdminChat",
		"Query"
	};

	private static readonly string[] Modulestxt = new string[]
	{
		"Warhead", "Networking", "Class change", "Permissions", "Administrative", "Game logic", "Data access", "FF Detector", "Throwable", "Door",
		"Elevator"
	};

	private static readonly Queue<ServerLogs.ServerLog> Queue = new Queue<ServerLogs.ServerLog>();

	private static readonly object LockObject = new object();

	public static readonly ConcurrentDictionary<string, IOutput> LiveLogOutput = new ConcurrentDictionary<string, IOutput>();

	private static Thread _appendThread;

	private static readonly int _maxlen;

	private static readonly int _modulemaxlen;

	private static volatile ServerLogs.LoggingState _state;

	public enum ServerLogType : byte
	{
		ConnectionUpdate,
		RemoteAdminActivity_GameChanging,
		RemoteAdminActivity_Misc,
		KillLog,
		GameEvent,
		InternalMessage,
		AuthRateLimit,
		Teamkill,
		Suicide,
		AdminChat,
		Query
	}

	public enum Modules : byte
	{
		Warhead,
		Networking,
		ClassChange,
		Permissions,
		Administrative,
		GameLogic,
		DataAccess,
		Detector,
		Throwable,
		Door,
		Elevator
	}

	private enum LoggingState : byte
	{
		Off,
		Standby,
		Write,
		Terminate,
		Restart
	}

	public readonly struct ServerLog : IEquatable<ServerLogs.ServerLog>
	{
		public ServerLog(string content, string type, string module, string time)
		{
			this.Content = content;
			this.Type = type;
			this.Module = module;
			this.Time = time;
		}

		public bool Equals(ServerLogs.ServerLog other)
		{
			return this.Content == other.Content && this.Type == other.Type && this.Module == other.Module && this.Time == other.Time;
		}

		public override bool Equals(object obj)
		{
			if (obj is ServerLogs.ServerLog)
			{
				ServerLogs.ServerLog serverLog = (ServerLogs.ServerLog)obj;
				return this.Equals(serverLog);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((this.Content != null) ? this.Content.GetHashCode() : 0) * 397) ^ ((this.Type != null) ? this.Type.GetHashCode() : 0)) * 397) ^ ((this.Module != null) ? this.Module.GetHashCode() : 0)) * 397) ^ ((this.Time != null) ? this.Time.GetHashCode() : 0);
		}

		public static bool operator ==(ServerLogs.ServerLog left, ServerLogs.ServerLog right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ServerLogs.ServerLog left, ServerLogs.ServerLog right)
		{
			return !left.Equals(right);
		}

		public readonly string Content;

		public readonly string Type;

		public readonly string Module;

		public readonly string Time;
	}
}
