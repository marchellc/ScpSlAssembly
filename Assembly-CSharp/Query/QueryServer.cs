using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cryptography;
using GameCore;
using Org.BouncyCastle.Security;

namespace Query;

internal class QueryServer
{
	private readonly TcpListener _listener1;

	private readonly TcpListener _listener2;

	private Thread _thr;

	private readonly int _port;

	private bool _queryStarted;

	private bool _serverStop;

	private static int _clientsLimit;

	internal readonly List<QueryUser> Users = new List<QueryUser>();

	internal readonly SecureRandom Random = new SecureRandom();

	internal readonly ushort BufferLength;

	internal readonly byte[] PasswordHash;

	internal readonly byte[] RxBuffer;

	internal readonly byte[] RxDecryptionBuffer;

	internal readonly byte[] RxNonceBuffer = new byte[32];

	internal static ushort TimeoutThreshold = 10000;

	internal static ulong QueryPermissions;

	internal static byte QueryKickPower;

	internal static int MaximumTimeDifference = 120;

	internal QueryServer(int port, string ip1, string ip2, string password)
	{
		if (port <= 0)
		{
			ServerConsole.AddLog($"Query port is set to {port}, but it can't be 0 or negative.", ConsoleColor.Red);
			return;
		}
		if (password.Length < 8)
		{
			ServerConsole.AddLog("Query password can't be shorter than 8 characters.", ConsoleColor.Red);
			return;
		}
		this.BufferLength = ConfigFile.ServerConfig.GetUShort("query_rx_buffer_size", 4096);
		if (this.BufferLength < 3072)
		{
			ServerConsole.AddLog("Query RX buffer can't be smaller than 3072.", ConsoleColor.Red);
			return;
		}
		this.PasswordHash = Sha.Sha256(password);
		this.RxBuffer = new byte[this.BufferLength];
		this.RxDecryptionBuffer = new byte[this.BufferLength];
		this._port = port;
		if (!string.IsNullOrWhiteSpace(ip1))
		{
			if (IPAddress.TryParse(ip1, out var address))
			{
				IPEndPoint iPEndPoint = new IPEndPoint(address, port);
				ServerConsole.AddLog($"Query bind endpoint 1 set to {iPEndPoint}");
				TcpListener tcpListener = new TcpListener(iPEndPoint);
				tcpListener.Server.NoDelay = true;
				this._listener1 = tcpListener;
				if (iPEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
				{
					this._listener1.Server.DualMode = ConfigFile.ServerConfig.GetBool("query_socket_ip1_dualmode");
				}
				if (ConfigFile.ServerConfig.GetBool("query_socket_ip1_linger"))
				{
					this._listener1.Server.LingerState = new LingerOption(enable: true, 0);
				}
			}
			else
			{
				ServerConsole.AddLog("Invalid IP address 1 for query server.", ConsoleColor.Red);
			}
		}
		if (string.IsNullOrWhiteSpace(ip2))
		{
			return;
		}
		if (IPAddress.TryParse(ip2, out var address2))
		{
			IPEndPoint iPEndPoint2 = new IPEndPoint(address2, port);
			ServerConsole.AddLog($"Query bind endpoint 2 set to {iPEndPoint2}");
			TcpListener tcpListener2 = new TcpListener(iPEndPoint2);
			tcpListener2.Server.NoDelay = true;
			this._listener2 = tcpListener2;
			if (iPEndPoint2.AddressFamily == AddressFamily.InterNetworkV6)
			{
				this._listener2.Server.DualMode = ConfigFile.ServerConfig.GetBool("query_socket_ip2_dualmode");
			}
			if (ConfigFile.ServerConfig.GetBool("query_socket_ip2_linger"))
			{
				this._listener2.Server.LingerState = new LingerOption(enable: true, 0);
			}
		}
		else
		{
			ServerConsole.AddLog("Invalid IP address 2 for query server.", ConsoleColor.Red);
		}
	}

	internal static void ReloadConfig()
	{
		QueryServer.QueryPermissions = ConfigFile.ServerConfig.GetULong("query_permissions", ulong.MaxValue);
		QueryServer.QueryKickPower = ConfigFile.ServerConfig.GetByte("query_kick_power", byte.MaxValue);
		QueryServer.TimeoutThreshold = ConfigFile.ServerConfig.GetUShort("query_timeout_time", 10000);
		QueryServer.MaximumTimeDifference = ConfigFile.ServerConfig.GetInt("query_maximum_time_difference", 120);
		QueryServer._clientsLimit = ConfigFile.ServerConfig.GetInt("query_clients_limit", 10);
		if (QueryServer.TimeoutThreshold < 500)
		{
			QueryServer.TimeoutThreshold = 500;
		}
		if (QueryServer.MaximumTimeDifference < 0)
		{
			QueryServer.MaximumTimeDifference = 0;
		}
		if (QueryServer._clientsLimit < 1)
		{
			QueryServer._clientsLimit = 1;
		}
	}

	internal void StartServer()
	{
		if (!this._queryStarted)
		{
			if (this._listener1 == null && this._listener2 == null)
			{
				ServerConsole.AddLog("Can't start query server - both listeners are null. Check if query bind addresses are correct.", ConsoleColor.Red);
				return;
			}
			this._queryStarted = true;
			this._thr = new Thread(HandleUsers)
			{
				IsBackground = true,
				Priority = ThreadPriority.BelowNormal
			};
			this._thr.Start();
		}
	}

	internal void StopServer()
	{
		if (this._queryStarted)
		{
			ServerConsole.AddLog("Stopping query server...");
			this._serverStop = true;
		}
	}

	private void HandleUsers()
	{
		ServerConsole.AddLog($"Starting query server on port {this._port} TCP...");
		try
		{
			this._listener1?.Start();
			this._listener2?.Start();
			while (!this._serverStop)
			{
				try
				{
					for (int num = this.Users.Count - 1; num >= 0; num--)
					{
						if (this.Users[num] == null)
						{
							this.Users.RemoveAt(num);
						}
						else
						{
							this.Users[num].Receive();
						}
					}
					if (this._listener1 != null && this._listener1.Pending())
					{
						this.AcceptSocket(this._listener1);
					}
					if (this._listener2 != null && this._listener2.Pending())
					{
						this.AcceptSocket(this._listener2);
					}
				}
				catch (Exception ex)
				{
					ServerConsole.AddLog("Query server loop error: " + ex.Message + "\n" + ex.StackTrace);
				}
				Thread.Sleep(15);
			}
			try
			{
				this._listener1?.Stop();
			}
			catch (Exception ex2)
			{
				ServerConsole.AddLog("Error stopping query listener 1: " + ex2.Message + "\n" + ex2.StackTrace);
			}
			try
			{
				this._listener2?.Stop();
			}
			catch (Exception ex3)
			{
				ServerConsole.AddLog("Error stopping query listener 1: " + ex3.Message + "\n" + ex3.StackTrace);
			}
			for (int num2 = this.Users.Count - 1; num2 >= 0; num2--)
			{
				this.Users[num2].DisconnectInternal(serverShutdown: true, force: true);
			}
			this.Users.Clear();
			ServerConsole.AddLog("Query server stopped.");
		}
		catch (Exception ex4)
		{
			ServerConsole.AddLog("Query server error: " + ex4.Message + "\n" + ex4.StackTrace);
		}
	}

	internal void DisconnectAllClients()
	{
		for (int num = this.Users.Count - 1; num >= 0; num--)
		{
			this.Users[num].Disconnect();
		}
	}

	private void AcceptSocket(TcpListener lst)
	{
		QueryUser queryUser = null;
		try
		{
			TcpClient tcpClient = lst.AcceptTcpClient();
			queryUser = new QueryUser(this, tcpClient);
			if (this.Users.Count >= QueryServer._clientsLimit)
			{
				ServerConsole.AddLog($"New query connection from {tcpClient.Client.RemoteEndPoint} on {tcpClient.Client.LocalEndPoint}, but the query server is full.", ConsoleColor.Yellow);
				queryUser.Send("Query server is full.", QueryMessage.ClientReceivedContentType.QueryMessage);
				queryUser.DisconnectInternal(serverShutdown: false, force: true);
			}
			else if (!queryUser.SendHandshake())
			{
				ServerConsole.AddLog($"New query connection from {tcpClient.Client.RemoteEndPoint} on {tcpClient.Client.LocalEndPoint}, but sending handshake failed.", ConsoleColor.Yellow);
			}
			else
			{
				this.Users.Add(queryUser);
				ServerConsole.AddLog($"New query connection from {tcpClient.Client.RemoteEndPoint} on {tcpClient.Client.LocalEndPoint}.");
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("An exception occured when accepting incoming query connection: " + ex.Message + "\n" + ex.StackTrace);
			queryUser?.DisconnectInternal(serverShutdown: false, force: true);
		}
	}
}
