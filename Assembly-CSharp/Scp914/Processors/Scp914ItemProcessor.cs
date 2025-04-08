using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace Scp914.Processors
{
	public abstract class Scp914ItemProcessor : MonoBehaviour
	{
		public abstract Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup);

		public virtual Scp914Result UpgradeInventoryItem(Scp914KnobSetting setting, ItemBase sourceItem)
		{
			ItemPickupBase itemPickupBase = sourceItem.ServerDropItem(false);
			Scp914Result scp914Result = this.UpgradePickup(setting, itemPickupBase);
			if (scp914Result.ResultingPickups == null || scp914Result.ResultingPickups.Length == 0)
			{
				return scp914Result;
			}
			InventoryInfo userInventory = sourceItem.OwnerInventory.UserInventory;
			this.ClearCombiner();
			foreach (ItemPickupBase itemPickupBase2 in scp914Result.ResultingPickups)
			{
				if (!(itemPickupBase2 == null))
				{
					SearchCompletor searchCompletor = SearchCompletor.FromPickup(sourceItem.Owner.searchCoordinator, itemPickupBase2, 3.4028234663852886E+38);
					if (searchCompletor.ValidateStart())
					{
						searchCompletor.Complete();
						ItemBase itemBase;
						if (userInventory.Items.TryGetValue(itemPickupBase2.Info.Serial, out itemBase))
						{
							this.AddResultToCombiner(itemBase);
						}
					}
					else
					{
						this.AddResultToCombiner(itemPickupBase2);
						itemPickupBase2.Position = sourceItem.Owner.transform.position;
						NetworkServer.Spawn(itemPickupBase2.gameObject, null);
					}
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
			if (resultingItem == null)
			{
				return;
			}
			Scp914ItemProcessor.ItemResultsToCombine.Add(resultingItem);
		}

		protected void AddResultToCombiner(ItemPickupBase resultingPickup)
		{
			if (resultingPickup == null)
			{
				return;
			}
			Scp914ItemProcessor.PickupResultsToCombine.Add(resultingPickup);
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
			Scp914Upgrader.OnUpgraded += Scp914ItemProcessor.OnUpgraded;
		}

		private static void OnUpgraded(Scp914Result res, Scp914KnobSetting knobSetting)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ItemBase[] resultingItems = res.ResultingItems;
			if (resultingItems != null)
			{
				resultingItems.ForEach(delegate(ItemBase ib)
				{
					Scp914ItemProcessor.ProcessResultingItem(ib, res, knobSetting);
				});
			}
			ItemPickupBase[] resultingPickups = res.ResultingPickups;
			if (resultingPickups == null)
			{
				return;
			}
			resultingPickups.ForEach(delegate(ItemPickupBase pickup)
			{
				Scp914ItemProcessor.ProcessResultingPickup(pickup, res, knobSetting);
			});
		}

		private static void ProcessResultingItem(ItemBase ib, Scp914Result res, Scp914KnobSetting knobSetting)
		{
			Firearm firearm = ib as Firearm;
			if (firearm == null || ib == null)
			{
				return;
			}
			Firearm firearm2;
			if (!firearm.ItemTypeId.TryGetTemplate(out firearm2))
			{
				return;
			}
			SubcomponentBase[] allSubcomponents = firearm2.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessScp914Creation(ib.ItemSerial, knobSetting, res, ib.ItemTypeId);
			}
		}

		private static void ProcessResultingPickup(ItemPickupBase pickup, Scp914Result res, Scp914KnobSetting knobSetting)
		{
			FirearmPickup firearmPickup = pickup as FirearmPickup;
			if (firearmPickup == null || pickup == null)
			{
				return;
			}
			ushort serial = firearmPickup.Info.Serial;
			SubcomponentBase[] allSubcomponents = firearmPickup.Template.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerProcessScp914Creation(serial, knobSetting, res, pickup.Info.ItemId);
			}
		}

		private static readonly List<ItemBase> ItemResultsToCombine = new List<ItemBase>();

		private static readonly List<ItemPickupBase> PickupResultsToCombine = new List<ItemPickupBase>();
	}
}
