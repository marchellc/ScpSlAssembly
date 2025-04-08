using System;
using System.Text;
using UnityEngine;

namespace Steam
{
	public static class SteamServerInfo
	{
		public static string ServerName { get; set; } = "Default server name.";

		public static string Version { get; set; } = "0.0.0";

		public static int OnlinePlayers { get; set; }

		public static int MaxPlayers { get; set; } = 25;

		public static int Bots { get; set; }

		public static string Tags { get; set; } = null;

		internal static ushort ServerPort { get; set; } = 7777;

		internal static long AppId { get; private set; } = 700330L;

		internal static string GameDirectory { get; private set; } = "scp secret laboratory";

		internal static string GameName { get; private set; } = "SCP: Secret Laboratory";

		internal static string MapName { get; private set; } = "Facility";

		internal static ServerType Type { get; private set; } = ServerType.Dedicated;

		internal static ServerOperativeSystem OperativeSystem
		{
			get
			{
				if (Application.platform != RuntimePlatform.LinuxPlayer)
				{
					return ServerOperativeSystem.Windows;
				}
				return ServerOperativeSystem.Linux;
			}
		}

		internal static bool IsPasswordProtected { get; private set; }

		internal static bool IsSecure { get; private set; }

		internal static byte[] Serialize()
		{
			byte[] array;
			using (CustomBinaryWriter customBinaryWriter = new CustomBinaryWriter())
			{
				customBinaryWriter.WriteInt(-1);
				customBinaryWriter.WriteByte(Encoding.UTF8.GetBytes("I")[0]);
				customBinaryWriter.WriteByte(17);
				customBinaryWriter.WriteString(SteamServerInfo.ServerName);
				customBinaryWriter.WriteString(SteamServerInfo.MapName);
				customBinaryWriter.WriteString(SteamServerInfo.GameDirectory);
				customBinaryWriter.WriteString(SteamServerInfo.GameName);
				customBinaryWriter.WriteShort((short)SteamServerInfo.AppId);
				byte maxValue = byte.MaxValue;
				customBinaryWriter.WriteByte((SteamServerInfo.OnlinePlayers > (int)maxValue) ? maxValue : ((byte)SteamServerInfo.OnlinePlayers));
				customBinaryWriter.WriteByte((SteamServerInfo.MaxPlayers > (int)maxValue) ? maxValue : ((byte)SteamServerInfo.MaxPlayers));
				customBinaryWriter.WriteByte((SteamServerInfo.Bots > (int)maxValue) ? maxValue : ((byte)SteamServerInfo.Bots));
				customBinaryWriter.WriteByte((byte)SteamServerInfo.Type);
				customBinaryWriter.WriteByte((byte)SteamServerInfo.OperativeSystem);
				customBinaryWriter.WriteByte(SteamServerInfo.IsPasswordProtected ? 1 : 0);
				customBinaryWriter.WriteByte(SteamServerInfo.IsSecure ? 1 : 0);
				customBinaryWriter.WriteString(SteamServerInfo.Version);
				if (string.IsNullOrEmpty(SteamServerInfo.Tags))
				{
					customBinaryWriter.WriteByte(145);
					customBinaryWriter.WriteShort((short)SteamServerInfo.ServerPort);
					customBinaryWriter.WriteLong(0L);
					customBinaryWriter.WriteLong(SteamServerInfo.AppId);
				}
				else
				{
					customBinaryWriter.WriteByte(177);
					customBinaryWriter.WriteShort((short)SteamServerInfo.ServerPort);
					customBinaryWriter.WriteLong(0L);
					customBinaryWriter.WriteString(SteamServerInfo.Tags);
					customBinaryWriter.WriteLong(SteamServerInfo.AppId);
				}
				array = customBinaryWriter.ToArray();
			}
			return array;
		}

		private const byte DefaultProtocol = 17;
	}
}
