using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace Scp914.Processors
{
	public class StandardItemProcessor : Scp914ItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			base.ClearCombiner();
			Vector3 vector = sourcePickup.Position + Scp914Controller.MoveVector;
			ItemType itemType = this.RandomOutput(setting, sourcePickup.Info.ItemId);
			this.ProcessPickup(sourcePickup, itemType, vector, setting);
			return base.GenerateResultFromCombiner(sourcePickup);
		}

		protected virtual void HandleNone(ItemPickupBase ipb, Vector3 newPosition)
		{
			ipb.DestroySelf();
		}

		protected virtual void HandleOldPickup(ItemPickupBase ipb, Vector3 newPosition)
		{
			ipb.DestroySelf();
		}

		private void ProcessPickup(ItemPickupBase sourcePickup, ItemType newType, Vector3 newPosition, Scp914KnobSetting setting)
		{
			if (newType == ItemType.None)
			{
				this.HandleNone(sourcePickup, newPosition);
				return;
			}
			ItemBase itemBase;
			if (newType == sourcePickup.Info.ItemId || !InventoryItemLoader.AvailableItems.TryGetValue(newType, out itemBase))
			{
				sourcePickup.transform.position = newPosition;
				if (this._fireUpgradeTrigger)
				{
					IUpgradeTrigger upgradeTrigger = sourcePickup as IUpgradeTrigger;
					if (upgradeTrigger != null)
					{
						upgradeTrigger.ServerOnUpgraded(setting);
					}
				}
				base.AddResultToCombiner(sourcePickup);
				return;
			}
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo
			{
				ItemId = newType,
				Serial = ItemSerialGenerator.GenerateNext(),
				WeightKg = itemBase.Weight
			};
			ItemPickupBase itemPickupBase = InventoryExtensions.ServerCreatePickup(itemBase, new PickupSyncInfo?(pickupSyncInfo), newPosition, true, null);
			base.AddResultToCombiner(itemPickupBase);
			this.HandleOldPickup(sourcePickup, newPosition);
		}

		private ItemType RandomOutput(Scp914KnobSetting setting, ItemType id)
		{
			ItemType[] array;
			switch (setting)
			{
			case Scp914KnobSetting.Rough:
				array = this._roughOutputs;
				break;
			case Scp914KnobSetting.Coarse:
				array = this._coarseOutputs;
				break;
			case Scp914KnobSetting.OneToOne:
				array = this._oneToOneOutputs;
				break;
			case Scp914KnobSetting.Fine:
				array = this._fineOutputs;
				break;
			case Scp914KnobSetting.VeryFine:
				array = this._veryFineOutputs;
				break;
			default:
				return id;
			}
			return array[global::UnityEngine.Random.Range(0, array.Length)];
		}

		[SerializeField]
		private ItemType[] _roughOutputs;

		[SerializeField]
		private ItemType[] _coarseOutputs;

		[SerializeField]
		private ItemType[] _oneToOneOutputs;

		[SerializeField]
		private ItemType[] _fineOutputs;

		[SerializeField]
		private ItemType[] _veryFineOutputs;

		[SerializeField]
		private bool _fireUpgradeTrigger;
	}
}
