using GameCore;
using Mirror;

namespace Security;

internal static class RateLimitCreator
{
	private static readonly string[] ServerRateLimits = new string[2] { "playerInteract", "commands" };

	private static readonly uint[] DefaultThresholds = new uint[2] { 60u, 150u };

	private static readonly uint[] DefaultWindows = new uint[2] { 5u, 3u };

	private static uint[][] _limits;

	private static int _limitsAmount;

	private static bool _init;

	private static RateLimit _dummy;

	private static RateLimit[] _dummyTable;

	internal static void Load()
	{
		RateLimitCreator._init = true;
		RateLimitCreator._limitsAmount = RateLimitCreator.ServerRateLimits.Length;
		RateLimitCreator._limits = new uint[RateLimitCreator._limitsAmount][];
		for (ushort num = 0; num < RateLimitCreator._limitsAmount; num++)
		{
			RateLimitCreator._limits[num] = new uint[2];
			RateLimitCreator._limits[num][0] = ConfigFile.ServerConfig.GetUInt("ratelimit_" + RateLimitCreator.ServerRateLimits[num] + "_threshold", RateLimitCreator.DefaultThresholds[num]);
			RateLimitCreator._limits[num][1] = ConfigFile.ServerConfig.GetUInt("ratelimit_" + RateLimitCreator.ServerRateLimits[num] + "_window", RateLimitCreator.DefaultWindows[num]);
		}
		RateLimitCreator._dummy = new DummyRateLimit();
		RateLimitCreator._dummyTable = new RateLimit[RateLimitCreator._limitsAmount];
		for (ushort num2 = 0; num2 < RateLimitCreator._limitsAmount; num2++)
		{
			RateLimitCreator._dummyTable[num2] = RateLimitCreator._dummy;
		}
		ServerConsole.AddLog("Rate limiting loaded");
	}

	internal static RateLimit[] CreateRateLimit(NetworkConnection connection, bool dummy = false)
	{
		if (NetworkServer.active && !dummy)
		{
			RateLimit[] array = new RateLimit[RateLimitCreator._limitsAmount];
			for (ushort num = 0; num < RateLimitCreator._limitsAmount; num++)
			{
				array[num] = new RateLimit((int)RateLimitCreator._limits[num][0], RateLimitCreator._limits[num][1], connection);
			}
			return array;
		}
		if (!RateLimitCreator._init)
		{
			RateLimitCreator.Load();
		}
		return RateLimitCreator._dummyTable;
	}
}
