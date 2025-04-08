using System;
using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson
{
	public static class ThirdpersonItemPoolManager
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += ThirdpersonItemPoolManager.SetupPools;
		}

		private static void SetupPools()
		{
			if (ThirdpersonItemPoolManager._poolsInitiated)
			{
				return;
			}
			foreach (KeyValuePair<ItemType, ItemBase> keyValuePair in InventoryItemLoader.AvailableItems)
			{
				PoolObject thirdpersonModel = keyValuePair.Value.ThirdpersonModel;
				if (thirdpersonModel != null)
				{
					PoolManager.Singleton.TryAddPool(thirdpersonModel);
				}
			}
			ThirdpersonItemPoolManager._poolsInitiated = true;
		}

		public static bool TryGet(InventorySubcontroller targetParent, ItemIdentifier item, out ThirdpersonItemBase result, bool restrictPoolingToItem = false)
		{
			if (restrictPoolingToItem)
			{
				ItemBase itemBase;
				if (!InventoryItemLoader.TryGetItem<ItemBase>(item.TypeId, out itemBase) || itemBase.ThirdpersonModel == null)
				{
					result = null;
					return false;
				}
				PoolManager.Singleton.TryAddPool(itemBase.ThirdpersonModel);
			}
			else
			{
				ThirdpersonItemPoolManager.SetupPools();
			}
			result = null;
			if (targetParent.ItemSpawnpoint == null)
			{
				return false;
			}
			ItemBase itemBase2;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(item.TypeId, out itemBase2))
			{
				return false;
			}
			if (itemBase2.ThirdpersonModel == null)
			{
				return false;
			}
			PoolObject poolObject;
			if (!PoolManager.Singleton.TryGetPoolObject(itemBase2.ThirdpersonModel.gameObject, out poolObject, false))
			{
				return false;
			}
			result = poolObject as ThirdpersonItemBase;
			result.Initialize(targetParent, item);
			result.SetupPoolObject();
			return true;
		}

		private static bool _poolsInitiated;
	}
}
