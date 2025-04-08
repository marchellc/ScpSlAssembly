using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using GameCore;
using UnityEngine;

public class CentralServer : MonoBehaviour
{
	public static string MasterUrl { get; internal set; }

	public static string StandardUrl { get; internal set; }

	public static string SelectedServer { get; internal set; }

	public static bool TestServer { get; internal set; }

	public static bool ServerSelected { get; set; }

	internal static string[] Servers { get; private set; }

	private void Start()
	{
		CentralServer.Init();
	}

	internal static void Init()
	{
		if (CentralServer._started)
		{
			return;
		}
		CentralServer._started = true;
		if (File.Exists(FileManager.GetAppFolder(true, false, "") + "testserver.txt"))
		{
			CentralServer.StandardUrl = "https://test.scpslgame.com/";
			CentralServer.MasterUrl = "https://test.scpslgame.com/";
			CentralServer.SelectedServer = "TEST";
			CentralServer.TestServer = true;
			CentralServer.ServerSelected = true;
			ServerConsole.AddLog("Using TEST central server: " + CentralServer.MasterUrl, ConsoleColor.Gray, false);
			return;
		}
		CentralServer.MasterUrl = "https://api.scpslgame.com/";
		CentralServer.StandardUrl = "https://api.scpslgame.com/";
		CentralServer.TestServer = false;
		CentralServer._lastReset = DateTime.MinValue;
		CentralServer.Servers = new string[0];
		CentralServer._workingServers = new List<string>();
		CentralServer.RefreshLock = new object();
		CentralServer._serversPath = FileManager.GetAppFolder(true, false, "") + "internal/";
		if (!Directory.Exists(CentralServer._serversPath))
		{
			Directory.CreateDirectory(CentralServer._serversPath);
		}
		CentralServer._serversPath += "CentralServers";
		if (File.Exists(CentralServer._serversPath))
		{
			CentralServer.Servers = FileManager.ReadAllLines(CentralServer._serversPath);
			if (CentralServer.Servers.Any((string server) => !Regex.IsMatch(server, "^[a-zA-Z0-9]*$")))
			{
				global::GameCore.Console.AddLog("Malformed server found on the list. Removing the list and redownloading it from api.scpslgame.com.", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
				CentralServer.Servers = new string[0];
				try
				{
					File.Delete(CentralServer._serversPath);
				}
				catch (Exception ex)
				{
					global::GameCore.Console.AddLog("Failed to delete malformed central server list.\nException: " + ex.Message, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				}
				new Thread(delegate
				{
					CentralServer.RefreshServerList(true, true);
				})
				{
					IsBackground = true,
					Priority = global::System.Threading.ThreadPriority.BelowNormal,
					Name = "SCP:SL Server list refreshing"
				}.Start();
				return;
			}
			CentralServer._workingServers = CentralServer.Servers.ToList<string>();
			if (!ServerStatic.IsDedicated)
			{
				global::GameCore.Console.AddLog("Cached central servers count: " + CentralServer.Servers.Length.ToString(), Color.grey, false, global::GameCore.Console.ConsoleLogType.Log);
			}
			if (CentralServer.Servers.Length != 0)
			{
				global::System.Random random = new global::System.Random();
				CentralServer.SelectedServer = CentralServer.Servers[random.Next(CentralServer.Servers.Length)];
				CentralServer.StandardUrl = "https://" + CentralServer.SelectedServer.ToLower() + ".scpslgame.com/";
				if (ServerStatic.IsDedicated)
				{
					ServerConsole.AddLog(string.Concat(new string[]
					{
						"Selected central server: ",
						CentralServer.SelectedServer,
						" (",
						CentralServer.StandardUrl,
						")"
					}), ConsoleColor.Gray, false);
				}
				else
				{
					global::GameCore.Console.AddLog(string.Concat(new string[]
					{
						"Selected central server: ",
						CentralServer.SelectedServer,
						" (",
						CentralServer.StandardUrl,
						")"
					}), Color.grey, false, global::GameCore.Console.ConsoleLogType.Log);
				}
			}
		}
		new Thread(delegate
		{
			CentralServer.RefreshServerList(true, true);
		})
		{
			IsBackground = true,
			Priority = global::System.Threading.ThreadPriority.BelowNormal,
			Name = "SCP:SL Server list refreshing"
		}.Start();
	}

	private static void RefreshServerList(bool planned = false, bool loop = false)
	{
		while (!CentralServer.Abort)
		{
			object refreshLock = CentralServer.RefreshLock;
			lock (refreshLock)
			{
				if (CentralServer.ServerSelected)
				{
					break;
				}
				if (CentralServer._workingServers.Count == 0)
				{
					if (CentralServer.Servers.Length == 0)
					{
						CentralServer.StandardUrl = "https://api.scpslgame.com/";
						CentralServer.SelectedServer = "Primary API";
					}
					else
					{
						CentralServer._workingServers = CentralServer.Servers.ToList<string>();
						CentralServer.StandardUrl = "https://" + CentralServer._workingServers[0] + ".scpslgame.com/";
						CentralServer.SelectedServer = CentralServer._workingServers[0];
					}
				}
				byte b = 1;
				while (!CentralServer.Abort)
				{
					if (b == 3)
					{
						break;
					}
					b += 1;
					try
					{
						string[] array = HttpQuery.Get(CentralServer.StandardUrl + "servers.php").Split(';', StringSplitOptions.None);
						if (File.Exists(CentralServer._serversPath))
						{
							File.Delete(CentralServer._serversPath);
						}
						FileManager.WriteToFile(array, CentralServer._serversPath, false);
						global::GameCore.Console.AddLog("Updated list of central servers.", Color.green, false, global::GameCore.Console.ConsoleLogType.Log);
						global::GameCore.Console.AddLog("Central servers count: " + array.Length.ToString(), Color.cyan, false, global::GameCore.Console.ConsoleLogType.Log);
						CentralServer.Servers = array;
						if (planned)
						{
							if (CentralServer.Servers.All((string srv) => srv != CentralServer.SelectedServer))
							{
								CentralServer._workingServers = CentralServer.Servers.ToList<string>();
								CentralServer.ChangeCentralServer(false);
							}
						}
						CentralServer.ServerSelected = true;
						break;
					}
					catch (Exception ex)
					{
						global::GameCore.Console.AddLog("Can't update central servers list!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
						global::GameCore.Console.AddLog("Error: " + ex.Message, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
						if (CentralServer.SelectedServer == "Primary API")
						{
							CentralServer.ServerSelected = true;
							break;
						}
						CentralServer.ChangeCentralServer(true);
					}
				}
			}
			if (!loop)
			{
				return;
			}
			uint num = 0U;
			while (num < 180U && !CentralServer.Abort)
			{
				Thread.Sleep(5000);
				num += 1U;
			}
		}
	}

	internal static bool ChangeCentralServer(bool remove)
	{
		CentralServer.ServerSelected = false;
		CentralServer.TestServer = false;
		if (CentralServer.SelectedServer == "Primary API")
		{
			if (CentralServer._lastReset >= DateTime.Now.AddMinutes(-2.0))
			{
				return false;
			}
			CentralServer.RefreshServerList(false, false);
			return true;
		}
		else
		{
			if (CentralServer._workingServers.Count == 0)
			{
				global::GameCore.Console.AddLog("All known central servers aren't working.", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
				CentralServer._workingServers.Add("API");
				CentralServer.SelectedServer = "Primary API";
				CentralServer.StandardUrl = "https://api.scpslgame.com/";
				global::GameCore.Console.AddLog(string.Concat(new string[]
				{
					"Changed central server: ",
					CentralServer.SelectedServer,
					" (",
					CentralServer.StandardUrl,
					")"
				}), Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
				return true;
			}
			if (remove && CentralServer._workingServers.Contains(CentralServer.SelectedServer))
			{
				CentralServer._workingServers.Remove(CentralServer.SelectedServer);
			}
			if (CentralServer._workingServers.Count == 0)
			{
				CentralServer._workingServers.Add("API");
				CentralServer.SelectedServer = "Primary API";
				CentralServer.StandardUrl = "https://api.scpslgame.com/";
				global::GameCore.Console.AddLog(string.Concat(new string[]
				{
					"Changed central server: ",
					CentralServer.SelectedServer,
					" (",
					CentralServer.StandardUrl,
					")"
				}), Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
				return true;
			}
			global::System.Random random = new global::System.Random();
			CentralServer.SelectedServer = CentralServer._workingServers[random.Next(0, CentralServer._workingServers.Count)];
			CentralServer.StandardUrl = "https://" + CentralServer.SelectedServer.ToLower() + ".scpslgame.com/";
			global::GameCore.Console.AddLog(string.Concat(new string[]
			{
				"Changed central server: ",
				CentralServer.SelectedServer,
				" (",
				CentralServer.StandardUrl,
				")"
			}), Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
			return true;
		}
	}

	public static object RefreshLock;

	private static string _serversPath;

	private static bool _started;

	private static List<string> _workingServers;

	private static DateTime _lastReset;

	internal static bool Abort;
}
