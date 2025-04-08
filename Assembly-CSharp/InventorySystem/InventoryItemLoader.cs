using System;
using System.Collections.Generic;
using InventorySystem.Items;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem
{
	public static class InventoryItemLoader
	{
		public static Dictionary<ItemType, ItemBase> AvailableItems
		{
			get
			{
				if (!InventoryItemLoader._loaded)
				{
					InventoryItemLoader.ForceReload();
				}
				return InventoryItemLoader._loadedItems;
			}
		}

		public static bool TryGetItem<T>(ItemType itemType, out T result) where T : ItemBase
		{
			ItemBase itemBase;
			if (InventoryItemLoader.AvailableItems.TryGetValue(itemType, out itemBase))
			{
				T t = itemBase as T;
				if (t != null)
				{
					result = t;
					return true;
				}
			}
			result = default(T);
			return false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += InventoryItemLoader.RegisterPrefabs;
		}

		private static void RegisterPrefabs()
		{
			HashSet<GameObject> hashSet = HashSetPool<GameObject>.Shared.Rent();
			foreach (ItemBase itemBase in InventoryItemLoader.AvailableItems.Values)
			{
				if (!(itemBase.PickupDropModel == null) && hashSet.Add(itemBase.PickupDropModel.gameObject))
				{
					NetworkClient.RegisterPrefab(itemBase.PickupDropModel.gameObject);
				}
			}
			Debug.Log(string.Concat(new string[]
			{
				"Successfully registered ",
				hashSet.Count.ToString(),
				" pickups for ",
				InventoryItemLoader.AvailableItems.Count.ToString(),
				" items."
			}));
			HashSetPool<GameObject>.Shared.Return(hashSet);
		}

		public static void ForceReload()
		{
			ItemType itemType = ItemType.None;
			try
			{
				InventoryItemLoader._loadedItems = new Dictionary<ItemType, ItemBase>();
				ItemBase[] array = Resources.LoadAll<ItemBase>("Defined Items");
				Array.Sort<ItemBase>(array, delegate(ItemBase x, ItemBase y)
				{
					int itemTypeId = (int)x.ItemTypeId;
					return itemTypeId.CompareTo((int)y.ItemTypeId);
				});
				foreach (ItemBase itemBase in array)
				{
					IHolidayItem holidayItem = itemBase as IHolidayItem;
					if ((holidayItem == null || holidayItem.IsAvailable) && itemBase.ItemTypeId != ItemType.None)
					{
						itemType = itemBase.ItemTypeId;
						InventoryItemLoader._loadedItems[itemBase.ItemTypeId] = itemBase;
						itemBase.OnTemplateReloaded(InventoryItemLoader._loaded);
					}
				}
				InventoryItemLoader._loaded = true;
			}
			catch (Exception ex)
			{
				Debug.LogError("Error while loading items from the resources folder. Last assigned item: " + itemType.ToString());
				Debug.LogException(ex);
				InventoryItemLoader._loaded = false;
			}
		}

		private static Dictionary<ItemType, ItemBase> _loadedItems = new Dictionary<ItemType, ItemBase>();

		private static bool _loaded;

		private const string ItemsDirectoryName = "Defined Items";
	}
}
