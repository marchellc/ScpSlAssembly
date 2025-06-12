using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

public class ServerShutdown : MonoBehaviour
{
	public enum ServerShutdownState : byte
	{
		NotInitiated,
		BroadcastingShutdown,
		ShuttingDown,
		Complete
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct ServerShutdownMessage : NetworkMessage
	{
	}

	private static float _c;

	public static ServerShutdownState ShutdownState { get; set; }

	[RuntimeInitializeOnLoadMethod]
	private static void InitOnLoad()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<ServerShutdownMessage>(HandleServerShutdown);
		};
	}

	private void Update()
	{
		switch (ServerShutdown.ShutdownState)
		{
		default:
			return;
		case ServerShutdownState.NotInitiated:
		case ServerShutdownState.Complete:
			return;
		case ServerShutdownState.BroadcastingShutdown:
			if (ServerShutdown._c > 400f)
			{
				ServerConsole.AddLog("Shutting down the server...", ConsoleColor.DarkCyan);
				ServerShutdown.ShutdownState = ServerShutdownState.ShuttingDown;
				ServerShutdown._c = 0f;
				NetworkServer.Shutdown();
				return;
			}
			break;
		case ServerShutdownState.ShuttingDown:
			if (ServerShutdown._c > 1000f)
			{
				ServerConsole.AddLog("Server shutdown completed.", ConsoleColor.DarkCyan);
				ServerShutdown.ShutdownState = ServerShutdownState.Complete;
				return;
			}
			break;
		}
		ServerShutdown._c += Time.unscaledDeltaTime;
	}

	internal static void Shutdown(bool noBroadcast = false)
	{
		if (ServerShutdown.ShutdownState != ServerShutdownState.NotInitiated)
		{
			return;
		}
		if (!NetworkServer.active)
		{
			ServerShutdown.ShutdownState = ServerShutdownState.Complete;
			return;
		}
		ServerConsole.AddLog("Server shutdown initiated.", ConsoleColor.DarkCyan);
		ServerShutdown.ShutdownState = ServerShutdownState.BroadcastingShutdown;
		IdleMode.SetIdleMode(state: false);
		CustomLiteNetLib4MirrorTransport.DelayConnections = true;
		CustomNetworkManager.QueryServer?.StopServer();
		if (!noBroadcast && !noBroadcast)
		{
			ServerConsole.AddLog("Broadcasting server shutdown to all connected players...", ConsoleColor.DarkCyan);
			NetworkServer.SendToAll(default(ServerShutdownMessage), 4, sendToReadyOnly: true);
		}
	}

	private static void HandleServerShutdown(ServerShutdownMessage ssm)
	{
	}
}
