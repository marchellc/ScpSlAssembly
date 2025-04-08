using System;
using GameCore;
using Mirror;

namespace Security
{
	internal static class RateLimitCreator
	{
		internal static void Load()
		{
			RateLimitCreator._init = true;
			RateLimitCreator._limitsAmount = RateLimitCreator.ServerRateLimits.Length;
			RateLimitCreator._limits = new uint[RateLimitCreator._limitsAmount][];
			ushort num = 0;
			while ((int)num < RateLimitCreator._limitsAmount)
			{
				RateLimitCreator._limits[(int)num] = new uint[2];
				RateLimitCreator._limits[(int)num][0] = ConfigFile.ServerConfig.GetUInt("ratelimit_" + RateLimitCreator.ServerRateLimits[(int)num] + "_threshold", RateLimitCreator.DefaultThresholds[(int)num]);
				RateLimitCreator._limits[(int)num][1] = ConfigFile.ServerConfig.GetUInt("ratelimit_" + RateLimitCreator.ServerRateLimits[(int)num] + "_window", RateLimitCreator.DefaultWindows[(int)num]);
				num += 1;
			}
			RateLimitCreator._dummy = new DummyRateLimit();
			RateLimitCreator._dummyTable = new RateLimit[RateLimitCreator._limitsAmount];
			ushort num2 = 0;
			while ((int)num2 < RateLimitCreator._limitsAmount)
			{
				RateLimitCreator._dummyTable[(int)num2] = RateLimitCreator._dummy;
				num2 += 1;
			}
			ServerConsole.AddLog("Rate limiting loaded", ConsoleColor.Gray, false);
		}

		internal static RateLimit[] CreateRateLimit(NetworkConnection connection, bool dummy = false)
		{
			if (NetworkServer.active && !dummy)
			{
				RateLimit[] array = new RateLimit[RateLimitCreator._limitsAmount];
				ushort num = 0;
				while ((int)num < RateLimitCreator._limitsAmount)
				{
					array[(int)num] = new RateLimit((int)RateLimitCreator._limits[(int)num][0], RateLimitCreator._limits[(int)num][1], connection);
					num += 1;
				}
				return array;
			}
			if (!RateLimitCreator._init)
			{
				RateLimitCreator.Load();
			}
			return RateLimitCreator._dummyTable;
		}

		private static readonly string[] ServerRateLimits = new string[] { "playerInteract", "commands" };

		private static readonly uint[] DefaultThresholds = new uint[] { 60U, 150U };

		private static readonly uint[] DefaultWindows = new uint[] { 5U, 3U };

		private static uint[][] _limits;

		private static int _limitsAmount;

		private static bool _init;

		private static RateLimit _dummy;

		private static RateLimit[] _dummyTable;
	}
}
