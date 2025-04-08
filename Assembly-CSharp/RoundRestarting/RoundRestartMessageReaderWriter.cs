using System;
using Mirror;

namespace RoundRestarting
{
	public static class RoundRestartMessageReaderWriter
	{
		public static RoundRestartMessage ReadRoundRestartMessage(this NetworkReader reader)
		{
			RoundRestartType roundRestartType = (RoundRestartType)reader.ReadByte();
			bool flag = false;
			bool flag2 = false;
			ushort num = 0;
			switch (roundRestartType)
			{
			case RoundRestartType.FullRestart:
				flag = reader.ReadBool();
				if (flag)
				{
					flag2 = reader.ReadBool();
				}
				break;
			case RoundRestartType.FastRestart:
				return new RoundRestartMessage(roundRestartType, 0f, num, false, false);
			case RoundRestartType.RedirectRestart:
				num = reader.ReadUShort();
				flag2 = reader.ReadBool();
				break;
			}
			return new RoundRestartMessage(roundRestartType, reader.ReadFloat(), num, flag, flag2);
		}

		public static void WriteRoundRestartMessage(this NetworkWriter writer, RoundRestartMessage msg)
		{
			writer.WriteByte((byte)msg.Type);
			switch (msg.Type)
			{
			case RoundRestartType.FullRestart:
				writer.WriteBool(msg.Reconnect);
				if (msg.Reconnect)
				{
					writer.WriteBool(msg.ExtendedReconnectionPeriod);
				}
				break;
			case RoundRestartType.FastRestart:
				return;
			case RoundRestartType.RedirectRestart:
				writer.WriteUShort(msg.NewPort);
				writer.WriteBool(msg.ExtendedReconnectionPeriod);
				break;
			}
			writer.WriteFloat(msg.TimeOffset);
		}
	}
}
