using System;
using System.Threading.Tasks;
using UnityEngine;

public static class Shutdown
{
	public static event Action OnQuit;

	public static void Quit(bool quit = true, bool suppressShutdownBroadcast = false)
	{
		if (Shutdown._quitting)
		{
			return;
		}
		Shutdown._quitting = true;
		Action onQuit = Shutdown.OnQuit;
		if (onQuit != null)
		{
			onQuit();
		}
		IdleMode.PauseIdleMode = true;
		ServerShutdown.Shutdown(suppressShutdownBroadcast);
		CentralServer.Abort = true;
		Shutdown.InternalShutdown(quit);
	}

	private static async void InternalShutdown(bool quit)
	{
		int i = 0;
		while ((i < 20 && ServerShutdown.ShutdownState != ServerShutdown.ServerShutdownState.Complete) || i < 6)
		{
			await Task.Delay(100);
			i++;
		}
		if (quit)
		{
			Application.Quit();
		}
	}

	private static bool _quitting;
}
