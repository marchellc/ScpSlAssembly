using GameCore;
using UnityEngine;

namespace CentralAuth;

public static class CentralAuthManager
{
	private static bool _initialized;

	private static bool _authDebugEnabled;

	public const int TokenVersion = 2;

	public static DistributionPlatform Platform { get; private set; }

	internal static void InitAuth()
	{
		if (!_initialized)
		{
			_initialized = true;
			CentralServer.Init();
			Platform = DistributionPlatform.Dedicated;
			Console.AddLog("Running as headless dedicated server. Skipping distribution platform detection.", new Color32(0, byte.MaxValue, 0, byte.MaxValue));
		}
	}
}
