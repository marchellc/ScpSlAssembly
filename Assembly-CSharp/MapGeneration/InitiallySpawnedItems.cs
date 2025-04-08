using System;
using System.Collections.Generic;
using InventorySystem.Items;
using UnityEngine;

namespace MapGeneration
{
	public class InitiallySpawnedItems : MonoBehaviour
	{
		private void Awake()
		{
			InitiallySpawnedItems.Singleton = this;
		}

		public bool IsInitiallySpawned(ushort _itemSerial)
		{
			return this._initiallySpawnedItemSerials.Contains(_itemSerial);
		}

		public bool IsInitiallySpawned(ItemIdentifier _item)
		{
			return this.IsInitiallySpawned(_item.SerialNumber);
		}

		public void AddInitial(ushort _itemSerial)
		{
			this._initiallySpawnedItemSerials.Add(_itemSerial);
		}

		public void AddInitial(ItemIdentifier _item)
		{
			this.AddInitial(_item.SerialNumber);
		}

		public void RemoveInitial(ushort _itemSerial)
		{
			this._initiallySpawnedItemSerials.Remove(_itemSerial);
		}

		public void RemoveInitial(ItemIdentifier _item)
		{
			this.RemoveInitial(_item.SerialNumber);
		}

		public void ClearAll()
		{
			this._initiallySpawnedItemSerials.Clear();
		}

		private HashSet<ushort> _initiallySpawnedItemSerials = new HashSet<ushort>();

		public static InitiallySpawnedItems Singleton;
	}
}
