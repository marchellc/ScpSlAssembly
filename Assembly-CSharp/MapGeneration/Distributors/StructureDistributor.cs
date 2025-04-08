using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class StructureDistributor : SpawnablesDistributorBase
	{
		protected override void PlaceSpawnables()
		{
			this.PrepareQueueForStructures();
			int num;
			StructureSpawnpoint structureSpawnpoint;
			while (this.TryGetNextStructure(out num, out structureSpawnpoint))
			{
				if (!(structureSpawnpoint == null))
				{
					this.SpawnStructure(this.Settings.SpawnableStructures[num], structureSpawnpoint.transform, structureSpawnpoint.TriggerDoorName);
				}
			}
		}

		private void PrepareQueueForStructures()
		{
			List<int> list = ListPool<int>.Shared.Rent();
			for (int i = 0; i < this.Settings.SpawnableStructures.Length; i++)
			{
				float num = global::UnityEngine.Random.Range(0f, 1f);
				int num2 = Mathf.FloorToInt(this.Settings.SpawnableStructures[i].MinMaxProbability.Evaluate(num));
				for (int j = 0; j < num2; j++)
				{
					if (j < this.Settings.SpawnableStructures[i].MinAmount)
					{
						this._queuedStructures.Enqueue(i);
					}
					else
					{
						list.Add(i);
					}
				}
			}
			while (list.Count > 0)
			{
				int num3 = global::UnityEngine.Random.Range(0, list.Count);
				this._queuedStructures.Enqueue(list[num3]);
				list.RemoveAt(num3);
			}
			ListPool<int>.Shared.Return(list);
		}

		private bool TryGetNextStructure(out int structureId, out StructureSpawnpoint spawnpoint)
		{
			spawnpoint = null;
			if (!this._queuedStructures.TryDequeue(out structureId))
			{
				return false;
			}
			if (this._missingSpawnpoints.Contains(structureId))
			{
				return true;
			}
			List<StructureSpawnpoint> list = ListPool<StructureSpawnpoint>.Shared.Rent();
			foreach (StructureSpawnpoint structureSpawnpoint in StructureSpawnpoint.AvailableInstances)
			{
				if (!(structureSpawnpoint == null) && structureSpawnpoint.CompatibleStructures.Contains(this.Settings.SpawnableStructures[structureId].StructureType))
				{
					list.Add(structureSpawnpoint);
				}
			}
			if (list.Count > 0)
			{
				StructureSpawnpoint structureSpawnpoint2 = list[global::UnityEngine.Random.Range(0, list.Count)];
				StructureSpawnpoint.AvailableInstances.Remove(structureSpawnpoint2);
				spawnpoint = structureSpawnpoint2;
			}
			else
			{
				this._missingSpawnpoints.Add(structureId);
			}
			return true;
		}

		private void SpawnStructure(SpawnableStructure structure, Transform tr, string doorName)
		{
			SpawnableStructure spawnableStructure = global::UnityEngine.Object.Instantiate<SpawnableStructure>(structure, tr.position, tr.rotation);
			spawnableStructure.transform.SetParent(tr);
			DoorNametagExtension doorNametagExtension;
			if (string.IsNullOrEmpty(doorName) || !DoorNametagExtension.NamedDoors.TryGetValue(doorName, out doorNametagExtension))
			{
				this.SpawnObject(spawnableStructure.gameObject);
				return;
			}
			base.RegisterUnspawnedObject(doorNametagExtension.TargetDoor, spawnableStructure.gameObject);
		}

		private readonly Queue<int> _queuedStructures = new Queue<int>();

		private readonly HashSet<int> _missingSpawnpoints = new HashSet<int>();
	}
}
