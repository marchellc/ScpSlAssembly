using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MapGeneration.Distributors
{
	public class ItemSpawnpoint : DistributorSpawnpointBase
	{
		public bool CanSpawn(ItemType[] items)
		{
			foreach (ItemType itemType in items)
			{
				if (itemType != ItemType.None && !this.CanSpawn(itemType))
				{
					return false;
				}
			}
			return true;
		}

		public bool CanSpawn(ItemType targetItem)
		{
			if (this._uses >= this.MaxUses)
			{
				return false;
			}
			ItemType[] acceptedItems = this._acceptedItems;
			for (int i = 0; i < acceptedItems.Length; i++)
			{
				if (acceptedItems[i] == targetItem)
				{
					return true;
				}
			}
			return false;
		}

		public Transform Occupy()
		{
			this._uses++;
			return this._positionVariants[global::UnityEngine.Random.Range(0, this._positionVariants.Length)];
		}

		private void Start()
		{
			if (this.AutospawnItem == ItemType.None)
			{
				ItemSpawnpoint.RandomInstances.Add(this);
				return;
			}
			ItemSpawnpoint.AutospawnInstances.Add(this);
		}

		private void OnDestroy()
		{
			if (this.AutospawnItem == ItemType.None)
			{
				ItemSpawnpoint.RandomInstances.Remove(this);
				return;
			}
			ItemSpawnpoint.AutospawnInstances.Remove(this);
		}

		public static readonly HashSet<ItemSpawnpoint> AutospawnInstances = new HashSet<ItemSpawnpoint>();

		public static readonly HashSet<ItemSpawnpoint> RandomInstances = new HashSet<ItemSpawnpoint>();

		public string TriggerDoorName;

		public ItemType AutospawnItem = ItemType.None;

		[Min(1f)]
		[FormerlySerializedAs("_maxUses")]
		public int MaxUses;

		[SerializeField]
		private ItemType[] _acceptedItems;

		[SerializeField]
		private Transform[] _positionVariants;

		private int _uses;
	}
}
