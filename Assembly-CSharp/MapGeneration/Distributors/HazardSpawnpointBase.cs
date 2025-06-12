using System.Collections.Generic;
using GameCore;
using Hazards;
using UnityEngine;

namespace MapGeneration.Distributors;

public abstract class HazardSpawnpointBase : DistributorSpawnpointBase
{
	public static readonly HashSet<HazardSpawnpointBase> Instances = new HashSet<HazardSpawnpointBase>();

	public EnvironmentalHazard HazardPrefab;

	public Vector3 SpawnScale = Vector3.one;

	public abstract string ServerConfigChanceName { get; }

	public float MaximumSpawnChance => ConfigFile.ServerConfig.GetFloat(this.ServerConfigChanceName);

	private new void Awake()
	{
		HazardSpawnpointBase.Instances.Add(this);
	}

	private void OnDestroy()
	{
		HazardSpawnpointBase.Instances.Remove(this);
	}
}
