using System;
using GameCore;
using UnityEngine;

namespace CentralAuth
{
	public static class CentralAuthManager
	{
		public static DistributionPlatform Platform { get; private set; }

		internal static void InitAuth()
		{
			if (CentralAuthManager._initialized)
			{
				return;
			}
			CentralAuthManager._initialized = true;
			CentralServer.Init();
			CentralAuthManager.Platform = DistributionPlatform.Dedicated;
			global::GameCore.Console.AddLog("Running as headless dedicated server. Skipping distribution platform detection.", new Color32(0, byte.MaxValue, 0, byte.MaxValue), false, global::GameCore.Console.ConsoleLogType.Log);
		}

		private static bool _initialized;

		private static bool _authDebugEnabled;

		public const int TokenVersion = 2;
	}
}
