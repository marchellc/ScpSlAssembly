using System;
using System.IO;
using Mirror;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	public static class StripdownNetworking
	{
		public static bool FileExportEnabled { get; set; }

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<StripdownNetworking.StripdownResponse>(new Action<StripdownNetworking.StripdownResponse>(StripdownNetworking.ProcessMessage), true);
			};
		}

		public static void ProcessMessage(StripdownNetworking.StripdownResponse file)
		{
			file.Lines.ForEach(delegate(string x)
			{
				Debug.Log(x);
			});
			if (!StripdownNetworking.FileExportEnabled)
			{
				return;
			}
			long num = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (num == StripdownNetworking._lastTime)
			{
				Debug.LogError("Stripdown file creation rate limited.");
				return;
			}
			File.WriteAllLines(string.Format("{0}/{1}.txt", Application.dataPath, num), file.Lines);
			StripdownNetworking._lastTime = num;
		}

		private static long _lastTime;

		public struct StripdownResponse : NetworkMessage
		{
			public string[] Lines;
		}
	}
}
