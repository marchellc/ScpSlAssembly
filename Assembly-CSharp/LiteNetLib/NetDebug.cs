using System;
using System.Diagnostics;
using UnityEngine;

namespace LiteNetLib
{
	public static class NetDebug
	{
		private static void WriteLogic(NetLogLevel logLevel, string str, params object[] args)
		{
			object debugLogLock = NetDebug.DebugLogLock;
			lock (debugLogLock)
			{
				if (NetDebug.Logger == null)
				{
					global::UnityEngine.Debug.Log(string.Format(str, args));
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
			NetDebug.WriteLogic(NetLogLevel.Trace, str, Array.Empty<object>());
		}

		[Conditional("DEBUG_MESSAGES")]
		internal static void Write(NetLogLevel level, string str)
		{
			NetDebug.WriteLogic(level, str, Array.Empty<object>());
		}

		[Conditional("DEBUG_MESSAGES")]
		[Conditional("DEBUG")]
		internal static void WriteForce(string str)
		{
			NetDebug.WriteLogic(NetLogLevel.Trace, str, Array.Empty<object>());
		}

		[Conditional("DEBUG_MESSAGES")]
		[Conditional("DEBUG")]
		internal static void WriteForce(NetLogLevel level, string str)
		{
			NetDebug.WriteLogic(level, str, Array.Empty<object>());
		}

		internal static void WriteError(string str)
		{
			NetDebug.WriteLogic(NetLogLevel.Error, str, Array.Empty<object>());
		}

		public static INetLogger Logger = null;

		private static readonly object DebugLogLock = new object();
	}
}
