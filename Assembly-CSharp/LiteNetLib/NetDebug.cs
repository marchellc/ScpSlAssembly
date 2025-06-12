using System.Diagnostics;
using UnityEngine;

namespace LiteNetLib;

public static class NetDebug
{
	public static INetLogger Logger = null;

	private static readonly object DebugLogLock = new object();

	private static void WriteLogic(NetLogLevel logLevel, string str, params object[] args)
	{
		lock (NetDebug.DebugLogLock)
		{
			if (NetDebug.Logger == null)
			{
				UnityEngine.Debug.Log(string.Format(str, args));
			}
			else
			{
				NetDebug.Logger.WriteNet(logLevel, str, args);
			}
		}
	}

	[Conditional("DEBUG_MESSAGES")]
	internal static void Write(string str)
	{
		NetDebug.WriteLogic(NetLogLevel.Trace, str);
	}

	[Conditional("DEBUG_MESSAGES")]
	internal static void Write(NetLogLevel level, string str)
	{
		NetDebug.WriteLogic(level, str);
	}

	[Conditional("DEBUG_MESSAGES")]
	[Conditional("DEBUG")]
	internal static void WriteForce(string str)
	{
		NetDebug.WriteLogic(NetLogLevel.Trace, str);
	}

	[Conditional("DEBUG_MESSAGES")]
	[Conditional("DEBUG")]
	internal static void WriteForce(NetLogLevel level, string str)
	{
		NetDebug.WriteLogic(level, str);
	}

	internal static void WriteError(string str)
	{
		NetDebug.WriteLogic(NetLogLevel.Error, str);
	}
}
