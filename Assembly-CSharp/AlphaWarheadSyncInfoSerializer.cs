using Mirror;

public static class AlphaWarheadSyncInfoSerializer
{
	public static void WriteAlphaWarheadSyncInfo(this NetworkWriter writer, AlphaWarheadSyncInfo value)
	{
		writer.WriteDouble(value.StartTime);
		if (value.StartTime != 0.0)
		{
			writer.WriteByte(value.ScenarioId);
			writer.WriteByte((byte)value.ScenarioType);
		}
	}

	public static AlphaWarheadSyncInfo ReadAlphaWarheadSyncInfo(this NetworkReader reader)
	{
		double num = reader.ReadDouble();
		AlphaWarheadSyncInfo result;
		if (num == 0.0)
		{
			result = default(AlphaWarheadSyncInfo);
			result.StartTime = 0.0;
			return result;
		}
		byte scenarioId = reader.ReadByte();
		byte scenarioType = reader.ReadByte();
		result = default(AlphaWarheadSyncInfo);
		result.ScenarioId = scenarioId;
		result.ScenarioType = (WarheadScenarioType)scenarioType;
		result.StartTime = num;
		return result;
	}
}
