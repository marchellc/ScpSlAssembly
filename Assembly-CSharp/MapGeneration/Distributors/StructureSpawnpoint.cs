using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.Distributors
{
	[DefaultExecutionOrder(-1)]
	public class StructureSpawnpoint : DistributorSpawnpointBase
	{
		private void Awake()
		{
			StructureSpawnpoint.AvailableInstances.Add(this);
		}

		private void OnDestroy()
		{
			StructureSpawnpoint.AvailableInstances.Remove(this);
		}

		public static readonly HashSet<StructureSpawnpoint> AvailableInstances = new HashSet<StructureSpawnpoint>();

		public StructureType[] CompatibleStructures;

		public string TriggerDoorName;
	}
}
