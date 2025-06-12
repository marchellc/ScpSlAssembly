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
	public static object RefreshLock;

	private static string _serversPath;

	private static bool _started;

	private static List<string> _workingServers;

	private static DateTime _lastReset;

	internal static bool Abort;

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
		if (File.Exists(FileManager.GetAppFolder() + "testserver.txt"))
		{
			CentralServer.StandardUrl = "https://test.scpslgame.com/";
			CentralServer.MasterUrl = "https://test.scpslgame.com/";
			CentralServer.SelectedServer = "TEST";
			CentralServer.TestServer = true;
			CentralServer.ServerSelected = true;
			ServerConsole.AddLog("Using TEST central server: " + CentralServer.MasterUrl);
			return;
		}
		CentralServer.MasterUrl = "https://api.scpslgame.com/";
		CentralServer.StandardUrl = "https://api.scpslgame.com/";
		CentralServer.TestServer = false;
		CentralServer._lastReset = DateTime.MinValue;
		CentralServer.Servers = new string[0];
		CentralServer._workingServers = new List<string>();
		CentralServer.RefreshLock = new object();
		CentralServer._serversPath = FileManager.GetAppFolder() + "internal/";
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
				GameCore.Console.AddLog("Malformed server found on the list. Removing the list and redownloading it from api.scpslgame.com.", Color.yellow);
				CentralServer.Servers = new string[0];
				try
				{
					File.Delete(CentralServer._serversPath);
				}
				catch (Exception ex)
				{
					GameCore.Console.AddLog("Failed to delete malformed central server list.\nException: " + ex.Message, Color.red);
				}
				Thread thread = new Thread((ThreadStart)delegate
				{
					CentralServer.RefreshServerList(planned: true, loop: true);
				});
				thread.IsBackground = true;
				thread.Priority = System.Threading.ThreadPriority.BelowNormal;
				thread.Name = "SCP:SL Server list refreshing";
				thread.Start();
				return;
			}
			CentralServer._workingServers = CentralServer.Servers.ToList();
			if (!ServerStatic.IsDedicated)
			{
				GameCore.Console.AddLog("Cached central servers count: " + CentralServer.Servers.Length, Color.grey);
			}
			if (CentralServer.Servers.Length != 0)
			{
				System.Random random = new System.Random();
				CentralServer.SelectedServer = CentralServer.Servers[random.Next(CentralServer.Servers.Length)];
				CentralServer.StandardUrl = "https://" + CentralServer.SelectedServer.ToLower() + ".scpslgame.com/";
				if (ServerStatic.IsDedicated)
				{
					ServerConsole.AddLog("Selected central server: " + CentralServer.SelectedServer + " (" + CentralServer.StandardUrl + ")");
				}
				else
				{
					GameCore.Console.AddLog("Selected central server: " + CentralServer.SelectedServer + " (" + CentralServer.StandardUrl + ")", Color.grey);
				}
			}
		}
		Thread thread2 = new Thread((ThreadStart)delegate
		{
			CentralServer.RefreshServerList(planned: true, loop: true);
		});
		thread2.IsBackground = true;
		thread2.Priority = System.Threading.ThreadPriority.BelowNormal;
		thread2.Name = "SCP:SL Server list refreshing";
		thread2.Start();
	}

	private static void RefreshServerList(bool planned = false, bool loop = false)
	{
		while (!CentralServer.Abort)
		{
			lock (CentralServer.RefreshLock)
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
						CentralServer._workingServers = CentralServer.Servers.ToList();
						CentralServer.StandardUrl = "https://" + CentralServer._workingServers[0] + ".scpslgame.com/";
						CentralServer.SelectedServer = CentralServer._workingServers[0];
					}
				}
				byte b = 1;
				while (!CentralServer.Abort && b != 3)
				{
					b++;
					try
					{
						string[] array = HttpQuery.Get(CentralServer.StandardUrl + "servers.php").Split(';');
						if (File.Exists(CentralServer._serversPath))
						{
							File.Delete(CentralServer._serversPath);
						}
						FileManager.WriteToFile(array, CentralServer._serversPath);
						GameCore.Console.AddLog("Updated list of central servers.", Color.green);
						GameCore.Console.AddLog("Central servers count: " + array.Length, Color.cyan);
						CentralServer.Servers = array;
						if (planned && CentralServer.Servers.All((string srv) => srv != CentralServer.SelectedServer))
						{
							CentralServer._workingServers = CentralServer.Servers.ToList();
							CentralServer.ChangeCentralServer(remove: false);
						}
						CentralServer.ServerSelected = true;
					}
					catch (Exception ex)
					{
						GameCore.Console.AddLog("Can't update central servers list!", Color.red);
						GameCore.Console.AddLog("Error: " + ex.Message, Color.red);
						if (CentralServer.SelectedServer == "Primary API")
						{
							CentralServer.ServerSelected = true;
							break;
						}
						CentralServer.ChangeCentralServer(remove: true);
						continue;
					}
					break;
				}
			}
			if (!loop)
			{
				break;
			}
			for (uint num = 0u; num < 180; num++)
			{
				if (CentralServer.Abort)
				{
					break;
				}
				Thread.Sleep(5000);
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
			CentralServer.RefreshServerList();
			return true;
		}
		if (CentralServer._workingServers.Count == 0)
		{
			GameCore.Console.AddLog("All known central servers aren't working.", Color.yellow);
			CentralServer._workingServers.Add("API");
			CentralServer.SelectedServer = "Primary API";
			CentralServer.StandardUrl = "https://api.scpslgame.com/";
			GameCore.Console.AddLog("Changed central server: " + CentralServer.SelectedServer + " (" + CentralServer.StandardUrl + ")", Color.yellow);
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
			GameCore.Console.AddLog("Changed central server: " + CentralServer.SelectedServer + " (" + CentralServer.StandardUrl + ")", Color.yellow);
			return true;
		}
		System.Random random = new System.Random();
		CentralServer.SelectedServer = CentralServer._workingServers[random.Next(0, CentralServer._workingServers.Count)];
		CentralServer.StandardUrl = "https://" + CentralServer.SelectedServer.ToLower() + ".scpslgame.com/";
		GameCore.Console.AddLog("Changed central server: " + CentralServer.SelectedServer + " (" + CentralServer.StandardUrl + ")", Color.yellow);
		return true;
	}
}
