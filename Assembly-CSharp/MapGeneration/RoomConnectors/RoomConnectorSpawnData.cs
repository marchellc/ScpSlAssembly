using System;
using UnityEngine;

namespace MapGeneration.RoomConnectors;

[Serializable]
public struct RoomConnectorSpawnData
{
	public SpawnableRoomConnectorType ConnectorType;

	public int MinInstances;

	public int MaxInstances;

	[Range(0f, 1f)]
	public float MinPercent;

	[Range(0f, 1f)]
	public float MaxPercent;

	public readonly int GetRequired(int totalSpawnpoints)
	{
		return Mathf.Max(this.MinInstances, Mathf.RoundToInt((float)totalSpawnpoints * this.MinPercent));
	}

	public readonly int GetOptional(int totalSpawnpoints)
	{
		int num = Mathf.Min(this.MaxInstances, Mathf.RoundToInt((float)totalSpawnpoints * this.MaxPercent));
		return Mathf.Max(0, num - this.GetRequired(totalSpawnpoints));
	}
}
