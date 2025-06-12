using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors;

public class ExperimentalWeaponLocker : Locker
{
	[Serializable]
	private struct GlobalLimit
	{
		public ItemType ItemType;

		public int Limit;
	}

	private static readonly Dictionary<ItemType, int> GloballySpawned = new Dictionary<ItemType, int>();

	[SerializeField]
	private GlobalLimit[] _globalLimits;

	public override void FillChamber(LockerChamber ch)
	{
		List<ItemType> list = ListPool<ItemType>.Shared.Rent();
		LockerLoot[] loot = base.Loot;
		foreach (LockerLoot lockerLoot in loot)
		{
			if (this.ValidateUnderGlobalLimit(lockerLoot.TargetItem))
			{
				list.Add(lockerLoot.TargetItem);
			}
		}
		if (list.Count > 0)
		{
			ItemType itemType = list.RandomItem();
			this.IncrementGloballySpawned(itemType);
			ch.SpawnItem(itemType, 1);
		}
		ListPool<ItemType>.Shared.Return(list);
	}

	private bool ValidateUnderGlobalLimit(ItemType it)
	{
		GlobalLimit[] globalLimits = this._globalLimits;
		for (int i = 0; i < globalLimits.Length; i++)
		{
			GlobalLimit globalLimit = globalLimits[i];
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
}
