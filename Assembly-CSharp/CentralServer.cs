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
		Init();
	}

	internal static void Init()
	{
		if (_started)
		{
			return;
		}
		_started = true;
		if (File.Exists(FileManager.GetAppFolder() + "testserver.txt"))
		{
			StandardUrl = "https://test.scpslgame.com/";
			MasterUrl = "https://test.scpslgame.com/";
			SelectedServer = "TEST";
			TestServer = true;
			ServerSelected = true;
			ServerConsole.AddLog("Using TEST central server: " + MasterUrl);
			return;
		}
		MasterUrl = "https://api.scpslgame.com/";
		StandardUrl = "https://api.scpslgame.com/";
		TestServer = false;
		_lastReset = DateTime.MinValue;
		Servers = new string[0];
		_workingServers = new List<string>();
		RefreshLock = new object();
		_serversPath = FileManager.GetAppFolder() + "internal/";
		if (!Directory.Exists(_serversPath))
		{
			Directory.CreateDirectory(_serversPath);
		}
		_serversPath += "CentralServers";
		if (File.Exists(_serversPath))
		{
			Servers = FileManager.ReadAllLines(_serversPath);
			if (Servers.Any((string server) => !Regex.IsMatch(server, "^[a-zA-Z0-9]*$")))
			{
				GameCore.Console.AddLog("Malformed server found on the list. Removing the list and redownloading it from api.scpslgame.com.", Color.yellow);
				Servers = new string[0];
				try
				{
					File.Delete(_serversPath);
				}
				catch (Exception ex)
				{
					GameCore.Console.AddLog("Failed to delete malformed central server list.\nException: " + ex.Message, Color.red);
				}
				Thread thread = new Thread((ThreadStart)delegate
				{
					RefreshServerList(planned: true, loop: true);
				});
				thread.IsBackground = true;
				thread.Priority = System.Threading.ThreadPriority.BelowNormal;
				thread.Name = "SCP:SL Server list refreshing";
				thread.Start();
				return;
			}
			_workingServers = Servers.ToList();
			if (!ServerStatic.IsDedicated)
			{
				GameCore.Console.AddLog("Cached central servers count: " + Servers.Length, Color.grey);
			}
			if (Servers.Length != 0)
			{
				System.Random random = new System.Random();
				SelectedServer = Servers[random.Next(Servers.Length)];
				StandardUrl = "https://" + SelectedServer.ToLower() + ".scpslgame.com/";
				if (ServerStatic.IsDedicated)
				{
					ServerConsole.AddLog("Selected central server: " + SelectedServer + " (" + StandardUrl + ")");
				}
				else
				{
					GameCore.Console.AddLog("Selected central server: " + SelectedServer + " (" + StandardUrl + ")", Color.grey);
				}
			}
		}
		Thread thread2 = new Thread((ThreadStart)delegate
		{
			RefreshServerList(planned: true, loop: true);
		});
		thread2.IsBackground = true;
		thread2.Priority = System.Threading.ThreadPriority.BelowNormal;
		thread2.Name = "SCP:SL Server list refreshing";
		thread2.Start();
	}

	private static void RefreshServerList(bool planned = false, bool loop = false)
	{
		while (!Abort)
		{
			lock (RefreshLock)
			{
				if (ServerSelected)
				{
					break;
				}
				if (_workingServers.Count == 0)
				{
					if (Servers.Length == 0)
					{
						StandardUrl = "https://api.scpslgame.com/";
						SelectedServer = "Primary API";
					}
					else
					{
						_workingServers = Servers.ToList();
						StandardUrl = "https://" + _workingServers[0] + ".scpslgame.com/";
						SelectedServer = _workingServers[0];
					}
				}
				byte b = 1;
				while (!Abort && b != 3)
				{
					b++;
					try
					{
						string[] array = HttpQuery.Get(StandardUrl + "servers.php").Split(';');
						if (File.Exists(_serversPath))
						{
							File.Delete(_serversPath);
						}
						FileManager.WriteToFile(array, _serversPath);
						GameCore.Console.AddLog("Updated list of central servers.", Color.green);
						GameCore.Console.AddLog("Central servers count: " + array.Length, Color.cyan);
						Servers = array;
						if (planned && Servers.All((string srv) => srv != SelectedServer))
						{
							_workingServers = Servers.ToList();
							ChangeCentralServer(remove: false);
						}
						ServerSelected = true;
					}
					catch (Exception ex)
					{
						GameCore.Console.AddLog("Can't update central servers list!", Color.red);
						GameCore.Console.AddLog("Error: " + ex.Message, Color.red);
						if (SelectedServer == "Primary API")
						{
							ServerSelected = true;
							break;
						}
						ChangeCentralServer(remove: true);
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
				if (Abort)
				{
					break;
				}
				Thread.Sleep(5000);
			}
		}
	}

	internal static bool ChangeCentralServer(bool remove)
	{
		ServerSelected = false;
		TestServer = false;
		if (SelectedServer == "Primary API")
		{
			if (_lastReset >= DateTime.Now.AddMinutes(-2.0))
			{
				return false;
			}
			RefreshServerList();
			return true;
		}
		if (_workingServers.Count == 0)
		{
			GameCore.Console.AddLog("All known central servers aren't working.", Color.yellow);
			_workingServers.Add("API");
			SelectedServer = "Primary API";
			StandardUrl = "https://api.scpslgame.com/";
			GameCore.Console.AddLog("Changed central server: " + SelectedServer + " (" + StandardUrl + ")", Color.yellow);
			return true;
		}
		if (remove && _workingServers.Contains(SelectedServer))
		{
			_workingServers.Remove(SelectedServer);
		}
		if (_workingServers.Count == 0)
		{
			_workingServers.Add("API");
			SelectedServer = "Primary API";
			StandardUrl = "https://api.scpslgame.com/";
			GameCore.Console.AddLog("Changed central server: " + SelectedServer + " (" + StandardUrl + ")", Color.yellow);
			return true;
		}
		System.Random random = new System.Random();
		SelectedServer = _workingServers[random.Next(0, _workingServers.Count)];
		StandardUrl = "https://" + SelectedServer.ToLower() + ".scpslgame.com/";
		GameCore.Console.AddLog("Changed central server: " + SelectedServer + " (" + StandardUrl + ")", Color.yellow);
		return true;
	}
}
