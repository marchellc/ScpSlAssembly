using System;
using System.Collections.Generic;
using InventorySystem.Items;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem;

public static class InventoryItemLoader
{
	private static Dictionary<ItemType, ItemBase> _loadedItems = new Dictionary<ItemType, ItemBase>();

	private static bool _loaded;

	private const string ItemsDirectoryName = "Defined Items";

	public static Dictionary<ItemType, ItemBase> AvailableItems
	{
		get
		{
			if (!_loaded)
			{
				ForceReload();
			}
			return _loadedItems;
		}
	}

	public static bool TryGetItem<T>(ItemType itemType, out T result) where T : ItemBase
	{
		if (!AvailableItems.TryGetValue(itemType, out var value) || !(value is T val))
		{
			result = null;
			return false;
		}
		result = val;
		return true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientStarted += RegisterPrefabs;
	}

	private static void RegisterPrefabs()
	{
		HashSet<GameObject> hashSet = HashSetPool<GameObject>.Shared.Rent();
		foreach (ItemBase value in AvailableItems.Values)
		{
			if (!(value.PickupDropModel == null) && hashSet.Add(value.PickupDropModel.gameObject))
			{
				NetworkClient.RegisterPrefab(value.PickupDropModel.gameObject);
			}
		}
		Debug.Log("Successfully registered " + hashSet.Count + " pickups for " + AvailableItems.Count + " items.");
		HashSetPool<GameObject>.Shared.Return(hashSet);
	}

	public static void ForceReload()
	{
		ItemType itemType = ItemType.None;
		try
		{
			_loadedItems = new Dictionary<ItemType, ItemBase>();
			ItemBase[] array = Resources.LoadAll<ItemBase>("Defined Items");
			Array.Sort(array, delegate(ItemBase x, ItemBase y)
			{
				int itemTypeId = (int)x.ItemTypeId;
				return itemTypeId.CompareTo((int)y.ItemTypeId);
			});
			ItemBase[] array2 = array;
			foreach (ItemBase itemBase in array2)
			{
				if (!(itemBase is IHolidayItem { IsAvailable: false }) && itemBase.ItemTypeId != ItemType.None)
				{
					itemType = itemBase.ItemTypeId;
					_loadedItems[itemBase.ItemTypeId] = itemBase;
					itemBase.OnTemplateReloaded(_loaded);
				}
			}
			_loaded = true;
		}
		catch (Exception exception)
		{
			Debug.LogError("Error while loading items from the resources folder. Last assigned item: " + itemType);
			Debug.LogException(exception);
			_loaded = false;
		}
	}
}
