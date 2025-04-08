using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class ExperimentalWeaponLocker : Locker
	{
		public override void FillChamber(LockerChamber ch)
		{
			List<ItemType> list = ListPool<ItemType>.Shared.Rent();
			foreach (LockerLoot lockerLoot in this.Loot)
			{
				if (this.ValidateUnderGlobalLimit(lockerLoot.TargetItem))
				{
					list.Add(lockerLoot.TargetItem);
				}
			}
			if (list.Count > 0)
			{
				ItemType itemType = list.RandomItem<ItemType>();
				this.IncrementGloballySpawned(itemType);
				ch.SpawnItem(itemType, 1);
			}
			ListPool<ItemType>.Shared.Return(list);
		}

		private bool ValidateUnderGlobalLimit(ItemType it)
		{
			foreach (ExperimentalWeaponLocker.GlobalLimit globalLimit in this._globalLimits)
			{
				if (globalLimit.ItemType == it)
				{
					return ExperimentalWeaponLocker.GloballySpawned.GetValueOrDefault(it) < globalLimit.Limit;
				}
			}
			return true;
		}

		private void IncrementGloballySpawned(ItemType it)
		{
			ExperimentalWeaponLocker.GloballySpawned[it] = ExperimentalWeaponLocker.GloballySpawned.GetValueOrDefault(it) + 1;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += ExperimentalWeaponLocker.GloballySpawned.Clear;
		}

		public override bool Weaved()
		{
			return true;
		}

		private static readonly Dictionary<ItemType, int> GloballySpawned = new Dictionary<ItemType, int>();

		[SerializeField]
		private ExperimentalWeaponLocker.GlobalLimit[] _globalLimits;

		[Serializable]
		private struct GlobalLimit
		{
			public ItemType ItemType;

			public int Limit;
		}
	}
}
