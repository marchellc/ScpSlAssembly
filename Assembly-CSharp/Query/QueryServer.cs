using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cryptography;
using GameCore;
using Org.BouncyCastle.Security;

namespace Query
{
	internal class QueryServer
	{
		internal QueryServer(int port, string ip1, string ip2, string password)
		{
			if (port <= 0)
			{
				ServerConsole.AddLog(string.Format("Query port is set to {0}, but it can't be 0 or negative.", port), ConsoleColor.Red, false);
				return;
			}
			if (password.Length < 8)
			{
				ServerConsole.AddLog("Query password can't be shorter than 8 characters.", ConsoleColor.Red, false);
				return;
			}
			this.BufferLength = ConfigFile.ServerConfig.GetUShort("query_rx_buffer_size", 4096);
			if (this.BufferLength < 3072)
			{
				ServerConsole.AddLog("Query RX buffer can't be smaller than 3072.", ConsoleColor.Red, false);
				return;
			}
			this.PasswordHash = Sha.Sha256(password);
			this.RxBuffer = new byte[(int)this.BufferLength];
			this.RxDecryptionBuffer = new byte[(int)this.BufferLength];
			this._port = port;
			if (!string.IsNullOrWhiteSpace(ip1))
			{
				IPAddress ipaddress;
				if (IPAddress.TryParse(ip1, out ipaddress))
				{
					IPEndPoint ipendPoint = new IPEndPoint(ipaddress, port);
					ServerConsole.AddLog(string.Format("Query bind endpoint 1 set to {0}", ipendPoint), ConsoleColor.Gray, false);
					this._listener1 = new TcpListener(ipendPoint)
					{
						Server = 
						{
							NoDelay = true
						}
					};
					if (ipendPoint.AddressFamily == AddressFamily.InterNetworkV6)
					{
						this._listener1.Server.DualMode = ConfigFile.ServerConfig.GetBool("query_socket_ip1_dualmode", false);
					}
					if (ConfigFile.ServerConfig.GetBool("query_socket_ip1_linger", false))
					{
						this._listener1.Server.LingerState = new LingerOption(true, 0);
					}
				}
				else
				{
					ServerConsole.AddLog("Invalid IP address 1 for query server.", ConsoleColor.Red, false);
				}
			}
			if (!string.IsNullOrWhiteSpace(ip2))
			{
				IPAddress ipaddress2;
				if (IPAddress.TryParse(ip2, out ipaddress2))
				{
					IPEndPoint ipendPoint2 = new IPEndPoint(ipaddress2, port);
					ServerConsole.AddLog(string.Format("Query bind endpoint 2 set to {0}", ipendPoint2), ConsoleColor.Gray, false);
					this._listener2 = new TcpListener(ipendPoint2)
					{
						Server = 
						{
							NoDelay = true
						}
					};
					if (ipendPoint2.AddressFamily == AddressFamily.InterNetworkV6)
					{
						this._listener2.Server.DualMode = ConfigFile.ServerConfig.GetBool("query_socket_ip2_dualmode", false);
					}
					if (ConfigFile.ServerConfig.GetBool("query_socket_ip2_linger", false))
					{
						this._listener2.Server.LingerState = new LingerOption(true, 0);
						return;
					}
				}
				else
				{
					ServerConsole.AddLog("Invalid IP address 2 for query server.", ConsoleColor.Red, false);
				}
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
			if (this._queryStarted)
			{
				return;
			}
			if (this._listener1 == null && this._listener2 == null)
			{
				ServerConsole.AddLog("Can't start query server - both listeners are null. Check if query bind addresses are correct.", ConsoleColor.Red, false);
				return;
			}
			this._queryStarted = true;
			this._thr = new Thread(new ThreadStart(this.HandleUsers))
			{
				IsBackground = true,
				Priority = ThreadPriority.BelowNormal
			};
			this._thr.Start();
		}

		internal void StopServer()
		{
			if (!this._queryStarted)
			{
				return;
			}
			ServerConsole.AddLog("Stopping query server...", ConsoleColor.Gray, false);
			this._serverStop = true;
		}

		private void HandleUsers()
		{
			ServerConsole.AddLog(string.Format("Starting query server on port {0} TCP...", this._port), ConsoleColor.Gray, false);
			try
			{
				TcpListener listener = this._listener1;
				if (listener != null)
				{
					listener.Start();
				}
				TcpListener listener2 = this._listener2;
				if (listener2 != null)
				{
					listener2.Start();
				}
				while (!this._serverStop)
				{
					try
					{
						for (int i = this.Users.Count - 1; i >= 0; i--)
						{
							if (this.Users[i] == null)
							{
								this.Users.RemoveAt(i);
							}
							else
							{
								this.Users[i].Receive();
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
						ServerConsole.AddLog("Query server loop error: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Gray, false);
					}
					Thread.Sleep(15);
				}
				try
				{
					TcpListener listener3 = this._listener1;
					if (listener3 != null)
					{
						listener3.Stop();
					}
				}
				catch (Exception ex2)
				{
					ServerConsole.AddLog("Error stopping query listener 1: " + ex2.Message + "\n" + ex2.StackTrace, ConsoleColor.Gray, false);
				}
				try
				{
					TcpListener listener4 = this._listener2;
					if (listener4 != null)
					{
						listener4.Stop();
					}
				}
				catch (Exception ex3)
				{
					ServerConsole.AddLog("Error stopping query listener 1: " + ex3.Message + "\n" + ex3.StackTrace, ConsoleColor.Gray, false);
				}
				for (int j = this.Users.Count - 1; j >= 0; j--)
				{
					this.Users[j].DisconnectInternal(true, true);
				}
				this.Users.Clear();
				ServerConsole.AddLog("Query server stopped.", ConsoleColor.Gray, false);
			}
			catch (Exception ex4)
			{
				ServerConsole.AddLog("Query server error: " + ex4.Message + "\n" + ex4.StackTrace, ConsoleColor.Gray, false);
			}
		}

		internal void DisconnectAllClients()
		{
			for (int i = this.Users.Count - 1; i >= 0; i--)
			{
				this.Users[i].Disconnect();
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
					ServerConsole.AddLog(string.Format("New query connection from {0} on {1}, but the query server is full.", tcpClient.Client.RemoteEndPoint, tcpClient.Client.LocalEndPoint), ConsoleColor.Yellow, false);
					queryUser.Send("Query server is full.", QueryMessage.ClientReceivedContentType.QueryMessage);
					queryUser.DisconnectInternal(false, true);
				}
				else if (!queryUser.SendHandshake())
				{
					ServerConsole.AddLog(string.Format("New query connection from {0} on {1}, but sending handshake failed.", tcpClient.Client.RemoteEndPoint, tcpClient.Client.LocalEndPoint), ConsoleColor.Yellow, false);
				}
				else
				{
					this.Users.Add(queryUser);
					ServerConsole.AddLog(string.Format("New query connection from {0} on {1}.", tcpClient.Client.RemoteEndPoint, tcpClient.Client.LocalEndPoint), ConsoleColor.Gray, false);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog("An exception occured when accepting incoming query connection: " + ex.Message + "\n" + ex.StackTrace, ConsoleColor.Gray, false);
				if (queryUser != null)
				{
					queryUser.DisconnectInternal(false, true);
				}
			}
		}

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
	}
}
