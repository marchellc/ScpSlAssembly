using System;
using Mirror;
using Query;
using UnityEngine;

public class ServerShutdown : MonoBehaviour
{
	[RuntimeInitializeOnLoadMethod]
	private static void InitOnLoad()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<ServerShutdown.ServerShutdownMessage>(new Action<ServerShutdown.ServerShutdownMessage>(ServerShutdown.HandleServerShutdown), true);
		};
	}

	public static ServerShutdown.ServerShutdownState ShutdownState { get; set; }

	private void Update()
	{
		switch (ServerShutdown.ShutdownState)
		{
		case ServerShutdown.ServerShutdownState.NotInitiated:
		case ServerShutdown.ServerShutdownState.Complete:
			return;
		case ServerShutdown.ServerShutdownState.BroadcastingShutdown:
			if (ServerShutdown._c > 400f)
			{
				ServerConsole.AddLog("Shutting down the server...", ConsoleColor.DarkCyan, false);
				ServerShutdown.ShutdownState = ServerShutdown.ServerShutdownState.ShuttingDown;
				ServerShutdown._c = 0f;
				NetworkServer.Shutdown();
				return;
			}
			break;
		case ServerShutdown.ServerShutdownState.ShuttingDown:
			if (ServerShutdown._c > 1000f)
			{
				ServerConsole.AddLog("Server shutdown completed.", ConsoleColor.DarkCyan, false);
				ServerShutdown.ShutdownState = ServerShutdown.ServerShutdownState.Complete;
				return;
			}
			break;
		default:
			return;
		}
		ServerShutdown._c += Time.unscaledDeltaTime;
	}

	internal static void Shutdown(bool noBroadcast = false)
	{
		if (ServerShutdown.ShutdownState != ServerShutdown.ServerShutdownState.NotInitiated)
		{
			return;
		}
		if (!NetworkServer.active)
		{
			ServerShutdown.ShutdownState = ServerShutdown.ServerShutdownState.Complete;
			return;
		}
		ServerConsole.AddLog("Server shutdown initiated.", ConsoleColor.DarkCyan, false);
		ServerShutdown.ShutdownState = ServerShutdown.ServerShutdownState.BroadcastingShutdown;
		IdleMode.SetIdleMode(false);
		CustomLiteNetLib4MirrorTransport.DelayConnections = true;
		QueryServer queryServer = CustomNetworkManager.QueryServer;
		if (queryServer != null)
		{
			queryServer.StopServer();
		}
		if (noBroadcast)
		{
			return;
		}
		if (noBroadcast)
		{
			return;
		}
		ServerConsole.AddLog("Broadcasting server shutdown to all connected players...", ConsoleColor.DarkCyan, false);
		NetworkServer.SendToAll<ServerShutdown.ServerShutdownMessage>(default(ServerShutdown.ServerShutdownMessage), 4, true);
	}

	private static void HandleServerShutdown(ServerShutdown.ServerShutdownMessage ssm)
	{
	}

	private static float _c;

	public enum ServerShutdownState : byte
	{
		NotInitiated,
		BroadcastingShutdown,
		ShuttingDown,
		Complete
	}

	private struct ServerShutdownMessage : NetworkMessage
	{
	}
}
