using System;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror
{
	[RequireComponent(typeof(LiteNetLib4MirrorTransport))]
	public class LiteNetLib4MirrorNetworkManager : NetworkManager
	{
		public override void Awake()
		{
			base.GetComponent<LiteNetLib4MirrorTransport>().InitializeTransport();
			base.Awake();
			LiteNetLib4MirrorNetworkManager.singleton = this;
		}

		public void StartClient(string ip, ushort port)
		{
			this.networkAddress = ip;
			this.maxConnections = 2;
			LiteNetLib4MirrorTransport.Singleton.clientAddress = ip;
			LiteNetLib4MirrorTransport.Singleton.port = port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = 2;
			this.StartClient();
		}

		public void StartHost(string serverIPv4BindAddress, string serverIPv6BindAddress, ushort port, ushort maxPlayers)
		{
			this.networkAddress = serverIPv4BindAddress;
			this.maxConnections = (int)maxPlayers;
			LiteNetLib4MirrorTransport.Singleton.clientAddress = serverIPv4BindAddress;
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = serverIPv4BindAddress;
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = serverIPv6BindAddress;
			LiteNetLib4MirrorTransport.Singleton.port = port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = maxPlayers;
			base.StartHost();
		}

		public void StartServer(string serverIPv4BindAddress, string serverIPv6BindAddress, ushort port, ushort maxPlayers)
		{
			this.networkAddress = serverIPv4BindAddress;
			this.maxConnections = (int)maxPlayers;
			LiteNetLib4MirrorTransport.Singleton.clientAddress = serverIPv4BindAddress;
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = serverIPv4BindAddress;
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = serverIPv6BindAddress;
			LiteNetLib4MirrorTransport.Singleton.port = port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = maxPlayers;
			base.StartServer();
		}

		public void StartHost(ushort port, ushort maxPlayers)
		{
			this.networkAddress = "127.0.0.1";
			this.maxConnections = (int)maxPlayers;
			LiteNetLib4MirrorTransport.Singleton.clientAddress = "127.0.0.1";
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = "0.0.0.0";
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = "::";
			LiteNetLib4MirrorTransport.Singleton.port = port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = maxPlayers;
			base.StartHost();
		}

		public void StartServer(ushort port, ushort maxPlayers)
		{
			this.networkAddress = "127.0.0.1";
			this.maxConnections = (int)maxPlayers;
			LiteNetLib4MirrorTransport.Singleton.serverIPv4BindAddress = "0.0.0.0";
			LiteNetLib4MirrorTransport.Singleton.serverIPv6BindAddress = "::";
			LiteNetLib4MirrorTransport.Singleton.port = port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = maxPlayers;
			base.StartServer();
		}

		public void DisconnectConnection(NetworkConnection conn, string message = null)
		{
			LiteNetLib4MirrorServer.DisconnectMessage = message;
			conn.Disconnect();
			LiteNetLib4MirrorServer.DisconnectMessage = null;
		}

		public new static LiteNetLib4MirrorNetworkManager singleton;
	}
}
