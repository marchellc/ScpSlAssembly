using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace Scp914.Processors;

public abstract class Scp914ItemProcessor : MonoBehaviour
{
	private static readonly List<ItemBase> ItemResultsToCombine = new List<ItemBase>();

	private static readonly List<ItemPickupBase> PickupResultsToCombine = new List<ItemPickupBase>();

	public abstract Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup);

	public virtual Scp914Result UpgradeInventoryItem(Scp914KnobSetting setting, ItemBase sourceItem)
	{
		ItemPickupBase sourcePickup = sourceItem.ServerDropItem(spawn: false);
		Scp914Result result = this.UpgradePickup(setting, sourcePickup);
		if (result.ResultingPickups == null || result.ResultingPickups.Length == 0)
		{
			return result;
		}
		InventoryInfo userInventory = sourceItem.OwnerInventory.UserInventory;
		this.ClearCombiner();
		ItemPickupBase[] resultingPickups = result.ResultingPickups;
		foreach (ItemPickupBase itemPickupBase in resultingPickups)
		{
			if (itemPickupBase == null)
			{
				continue;
			}
			ISearchCompletor searchCompletor = itemPickupBase.GetSearchCompletor(sourceItem.Owner.searchCoordinator, float.MaxValue);
			if (searchCompletor.ValidateStart())
			{
				searchCompletor.Complete();
				if (userInventory.Items.TryGetValue(itemPickupBase.Info.Serial, out var value))
				{
					this.AddResultToCombiner(value);
				}
			}
			else
			{
				this.AddResultToCombiner(itemPickupBase);
				itemPickupBase.Position = sourceItem.Owner.transform.position;
				NetworkServer.Spawn(itemPickupBase.gameObject);
			}
		}
		return this.GenerateResultFromCombiner(sourceItem);
	}

	protected void ClearCombiner()
	{
		Scp914ItemProcessor.ItemResultsToCombine.Clear();
		Scp914ItemProcessor.PickupResultsToCombine.Clear();
	}

	protected void AddResultToCombiner(ItemBase resultingItem)
	{
		if (!(resultingItem == null))
		{
			Scp914ItemProcessor.ItemResultsToCombine.Add(resultingItem);
		}
	}

	protected void AddResultToCombiner(ItemPickupBase resultingPickup)
	{
		if (!(resultingPickup == null))
		{
			Scp914ItemProcessor.PickupResultsToCombine.Add(resultingPickup);
		}
	}

	protected Scp914Result GenerateResultFromCombiner(ItemBase oldItem)
	{
		return new Scp914Result(oldItem, (Scp914ItemProcessor.ItemResultsToCombine.Count > 0) ? Scp914ItemProcessor.ItemResultsToCombine.ToArray() : null, (Scp914ItemProcessor.PickupResultsToCombine.Count > 0) ? Scp914ItemProcessor.PickupResultsToCombine.ToArray() : null);
	}

	protected Scp914Result GenerateResultFromCombiner(ItemPickupBase oldPickup)
	{
		return new Scp914Result(oldPickup, (Scp914ItemProcessor.ItemResultsToCombine.Count > 0) ? Scp914ItemProcessor.ItemResultsToCombine.ToArray() : null, (Scp914ItemProcessor.PickupResultsToCombine.Count > 0) ? Scp914ItemProcessor.PickupResultsToCombine.ToArray() : null);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Scp914Upgrader.OnUpgraded += OnUpgraded;
	}

	private static void OnUpgraded(Scp914Result res, Scp914KnobSetting knobSetting)
	{
		if (NetworkServer.active)
		{
			res.ResultingItems?.ForEach(delegate(ItemBase ib)
			{
				Scp914ItemProcessor.ProcessResultingItem(ib, res, knobSetting);
			});
			res.ResultingPickups?.ForEach(delegate(ItemPickupBase pickup)
			{
				Scp914ItemProcessor.ProcessResultingPickup(pickup, res, knobSetting);
			});
		}
	}

	private static void ProcessResultingItem(ItemBase ib, Scp914Result res, Scp914KnobSetting knobSetting)
	{
		if (ib is Firearm firearm && !(ib == null) && firearm.ItemTypeId.TryGetTemplate<Firearm>(out var item))
		{
			SubcomponentBase[] allSubcomponents = item.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessScp914Creation(ib.ItemSerial, knobSetting, res, ib.ItemTypeId);
			}
		}
	}

	private static void ProcessResultingPickup(ItemPickupBase pickup, Scp914Result res, Scp914KnobSetting knobSetting)
	{
		if (pickup is FirearmPickup firearmPickup && !(pickup == null))
		{
			ushort serial = firearmPickup.Info.Serial;
			SubcomponentBase[] allSubcomponents = firearmPickup.Template.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessScp914Creation(serial, knobSetting, res, pickup.Info.ItemId);
			}
		}
	}
}
