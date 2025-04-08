using System;
using Mirror;
using UnityEngine;

namespace Subtitles
{
	public static class SubtitleMessageExtensions
	{
		public static void Serialize(this NetworkWriter writer, SubtitleMessage value)
		{
			SubtitlePart[] subtitleParts = value.SubtitleParts;
			if (subtitleParts == null || subtitleParts.Length == 0)
			{
				return;
			}
			writer.WriteByte((byte)subtitleParts.Length);
			foreach (SubtitlePart subtitlePart in subtitleParts)
			{
				writer.WriteByte((subtitlePart.OptionalData == null) ? 0 : ((byte)subtitlePart.OptionalData.Length));
				if (subtitlePart.OptionalData != null && subtitlePart.OptionalData.Length != 0)
				{
					for (int j = 0; j < subtitlePart.OptionalData.Length; j++)
					{
						writer.WriteString(subtitlePart.OptionalData[j]);
					}
				}
				writer.WriteByte((byte)subtitlePart.Subtitle);
			}
		}

		public static SubtitleMessage Deserialize(this NetworkReader reader)
		{
			int num = (int)reader.ReadByte();
			SubtitlePart[] array = new SubtitlePart[num];
			for (int i = 0; i < num; i++)
			{
				int num2 = (int)reader.ReadByte();
				string[] array2 = new string[num2];
				for (int j = 0; j < num2; j++)
				{
					array2[j] = reader.ReadString();
				}
				array[i] = new SubtitlePart((SubtitleType)reader.ReadByte(), (num2 == 0) ? null : array2);
			}
			return new SubtitleMessage
			{
				SubtitleParts = array
			};
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += SubtitleMessageExtensions.RegisterHandlers;
		}

		private static void RegisterHandlers()
		{
			NetworkClient.ReplaceHandler<SubtitleMessage>(new Action<SubtitleMessage>(SubtitleMessageExtensions.ClientMessageReceived), true);
		}

		private static void ClientMessageReceived(SubtitleMessage msg)
		{
		}
	}
}
