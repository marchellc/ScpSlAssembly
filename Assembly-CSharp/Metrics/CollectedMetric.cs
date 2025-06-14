using System;
using GameCore;

namespace Metrics;

public struct CollectedMetric : IJsonSerializable
{
	public string Type;

	public string Data;

	public float RoundTime;

	public CollectedMetric(Type type, string jsonData)
	{
		this.Type = type.Name;
		this.Data = jsonData;
		this.RoundTime = (float)RoundStart.RoundLength.TotalSeconds;
	}
}
