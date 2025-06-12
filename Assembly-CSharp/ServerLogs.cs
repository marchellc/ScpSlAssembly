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

	public readonly struct ServerLog : IEquatable<ServerLog>
	{
		public readonly string Content;

		public readonly string Type;

		public readonly string Module;

		public readonly string Time;

		public ServerLog(string content, string type, string module, string time)
		{
			this.Content = content;
			this.Type = type;
			this.Module = module;
			this.Time = time;
		}

		public bool Equals(ServerLog other)
		{
			if (this.Content == other.Content && this.Type == other.Type && this.Module == other.Module)
			{
				return this.Time == other.Time;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ServerLog other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((this.Content != null) ? this.Content.GetHashCode() : 0) * 397) ^ ((this.Type != null) ? this.Type.GetHashCode() : 0)) * 397) ^ ((this.Module != null) ? this.Module.GetHashCode() : 0)) * 397) ^ ((this.Time != null) ? this.Time.GetHashCode() : 0);
		}

		public static bool operator ==(ServerLog left, ServerLog right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ServerLog left, ServerLog right)
		{
			return !left.Equals(right);
		}
	}

	private static readonly string[] Txt;

	private static readonly string[] Modulestxt;

	private static readonly Queue<ServerLog> Queue;

	private static readonly object LockObject;

	public static readonly ConcurrentDictionary<string, IOutput> LiveLogOutput;

	private static Thread _appendThread;

	private static readonly int _maxlen;

	private static readonly int _modulemaxlen;

	private static volatile LoggingState _state;

	static ServerLogs()
	{
		ServerLogs.Txt = new string[11]
		{
			"Connection update", "Remote Admin", "Remote Admin - Misc", "Kill", "Game Event", "Internal", "Auth Rate Limit", "Teamkill", "Suicide", "AdminChat",
			"Query"
		};
		ServerLogs.Modulestxt = new string[11]
		{
			"Warhead", "Networking", "Class change", "Permissions", "Administrative", "Game logic", "Data access", "FF Detector", "Throwable", "Door",
			"Elevator"
		};
		ServerLogs.Queue = new Queue<ServerLog>();
		ServerLogs.LockObject = new object();
		ServerLogs.LiveLogOutput = new ConcurrentDictionary<string, IOutput>();
		string[] txt = ServerLogs.Txt;
		foreach (string text in txt)
		{
			ServerLogs._maxlen = Math.Max(ServerLogs._maxlen, text.Length);
		}
		txt = ServerLogs.Modulestxt;
		foreach (string text2 in txt)
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
		if (ServerLogs._state != LoggingState.Off)
		{
			ServerLogs._state = LoggingState.Restart;
			return;
		}
		Thread appendThread = ServerLogs._appendThread;
		if (appendThread == null || !appendThread.IsAlive)
		{
			ServerLogs._appendThread = new Thread(AppendLog)
			{
				Name = "Saving server logs to file",
				Priority = System.Threading.ThreadPriority.BelowNormal,
				IsBackground = true
			};
			ServerLogs._appendThread.Start();
		}
	}

	public static void AddLog(Modules module, string msg, ServerLogType type, bool init = false)
	{
		string time = TimeBehaviour.Rfc3339Time();
		lock (ServerLogs.LockObject)
		{
			ServerLogs.Queue.Enqueue(new ServerLog(msg, ServerLogs.Txt[(uint)type], ServerLogs.Modulestxt[(uint)module], time));
		}
		if (!init)
		{
			ServerLogs._state = LoggingState.Write;
		}
	}

	private void OnApplicationQuit()
	{
		ServerLogs._state = LoggingState.Terminate;
	}

	private static void AppendLog()
	{
		ServerLogs._state = LoggingState.Standby;
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		while (ServerLogs._state != LoggingState.Terminate)
		{
			lock (ServerLogs.LockObject)
			{
				ServerLogs.Queue.Clear();
				ServerLogs._state = LoggingState.Standby;
			}
			while (!NetworkServer.active)
			{
				if (ServerLogs._state == LoggingState.Terminate)
				{
					ServerLogs._state = LoggingState.Off;
					StringBuilderPool.Shared.Return(stringBuilder);
					return;
				}
				Thread.Sleep(200);
			}
			string text = TimeBehaviour.FormatTime("yyyy-MM-dd HH.mm.ss");
			string text2 = LiteNetLib4MirrorTransport.Singleton.port.ToString();
			ServerLogs.AddLog(Modules.GameLogic, "Started logging.", ServerLogType.InternalMessage, init: true);
			ServerLogs.AddLog(Modules.GameLogic, "Game version: " + GameCore.Version.VersionString + ".", ServerLogType.InternalMessage, init: true);
			ServerLogs.AddLog(Modules.GameLogic, "Build type: " + GameCore.Version.BuildType.ToString() + ".", ServerLogType.InternalMessage, init: true);
			ServerLogs.AddLog(Modules.GameLogic, "Build timestamp: 2025-06-09 00:37:28Z.", ServerLogType.InternalMessage, init: true);
			ServerLogs.AddLog(Modules.GameLogic, "Headless: " + PlatformInfo.singleton.IsHeadless + ".", ServerLogType.InternalMessage, init: true);
			while (NetworkServer.active && ServerLogs._state != LoggingState.Terminate && ServerLogs._state != LoggingState.Restart)
			{
				Thread.Sleep(100);
				if (ServerLogs._state == LoggingState.Standby)
				{
					continue;
				}
				if (!Directory.Exists(FileManager.GetAppFolder()))
				{
					return;
				}
				if (!Directory.Exists(FileManager.GetAppFolder() + "ServerLogs"))
				{
					Directory.CreateDirectory(FileManager.GetAppFolder() + "ServerLogs");
				}
				if (!Directory.Exists(FileManager.GetAppFolder() + "ServerLogs/" + text2))
				{
					Directory.CreateDirectory(FileManager.GetAppFolder() + "ServerLogs/" + text2);
				}
				lock (ServerLogs.LockObject)
				{
					ServerLog result;
					while (ServerLogs.Queue.TryDequeue(out result))
					{
						string text3 = result.Time + " | " + ServerLogs.ToMax(result.Type, ServerLogs._maxlen) + " | " + ServerLogs.ToMax(result.Module, ServerLogs._modulemaxlen) + " | " + result.Content;
						stringBuilder.AppendLine(text3);
						ServerLogs.PrintOnOutputs(text3);
					}
				}
				using (StreamWriter streamWriter = new StreamWriter(FileManager.GetAppFolder() + "ServerLogs/" + text2 + "/Round " + text + ".txt", append: true))
				{
					streamWriter.Write(stringBuilder.ToString());
				}
				stringBuilder.Clear();
				LoggingState state = ServerLogs._state;
				if (state == LoggingState.Terminate || state == LoggingState.Restart)
				{
					break;
				}
				ServerLogs._state = LoggingState.Standby;
			}
		}
		ServerLogs._state = LoggingState.Off;
		StringBuilderPool.Shared.Return(stringBuilder);
	}

	private static void PrintOnOutputs(string text)
	{
		try
		{
			if (ServerLogs.LiveLogOutput == null)
			{
				return;
			}
			foreach (KeyValuePair<string, IOutput> item in ServerLogs.LiveLogOutput)
			{
				IOutput value;
				try
				{
					if (item.Value == null || !item.Value.Available())
					{
						ServerLogs.LiveLogOutput.TryRemove(item.Key, out value);
					}
					else if (item.Value is ServerConsoleSender)
					{
						ServerConsole.AddLog(text, ConsoleColor.Gray, hideFromOutputs: true);
					}
					else
					{
						item.Value.Print(text);
					}
				}
				catch
				{
					ServerLogs.LiveLogOutput.TryRemove(item.Key, out value);
				}
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Failed to print log to outputs: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Red);
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
}
