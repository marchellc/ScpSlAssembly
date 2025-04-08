using System;
using Mirror;

public static class AlphaWarheadSyncInfoSerializer
{
	public static void WriteAlphaWarheadSyncInfo(this NetworkWriter writer, AlphaWarheadSyncInfo value)
	{
		writer.WriteDouble(value.StartTime);
		if (value.StartTime == 0.0)
		{
			return;
		}
		writer.WriteByte(value.ScenarioId);
		writer.WriteByte((byte)value.ScenarioType);
	}

	public static AlphaWarheadSyncInfo ReadAlphaWarheadSyncInfo(this NetworkReader reader)
	{
		double num = reader.ReadDouble();
		if (num == 0.0)
		{
			return new AlphaWarheadSyncInfo
			{
				StartTime = 0.0
			};
		}
		byte b = reader.ReadByte();
		byte b2 = reader.ReadByte();
		return new AlphaWarheadSyncInfo
		{
			ScenarioId = b,
			ScenarioType = (WarheadScenarioType)b2,
			StartTime = num
		};
	}
}
