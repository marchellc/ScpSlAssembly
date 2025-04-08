using System;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace Scp914.Processors
{
	public class FirearmItemProcessor : Scp914ItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase ipb)
		{
			base.ClearCombiner();
			foreach (ItemType itemType in this.GetItems(setting, ipb.Info.ItemId))
			{
				this.UpgradePickup(itemType, ipb);
			}
			return base.GenerateResultFromCombiner(ipb);
		}

		private void UpgradePickup(ItemType newType, ItemPickupBase source)
		{
			FirearmItemProcessor.<>c__DisplayClass7_0 CS$<>8__locals1 = new FirearmItemProcessor.<>c__DisplayClass7_0();
			CS$<>8__locals1.<>4__this = this;
			if (!(source is FirearmPickup))
			{
				throw new InvalidOperationException("Attempting to use FirearmItemProcessor on a non-firearm item.");
			}
			ItemIdentifier itemIdentifier = new ItemIdentifier(source.Info.ItemId, source.Info.Serial);
			CS$<>8__locals1.newPos = source.transform.position + Scp914Controller.MoveVector;
			ItemBase itemBase;
			if (!newType.TryGetTemplate(out itemBase))
			{
				if (newType == ItemType.None)
				{
					source.DestroySelf();
					return;
				}
				source.transform.position = CS$<>8__locals1.newPos;
				base.AddResultToCombiner(source);
				return;
			}
			else
			{
				if (newType == itemIdentifier.TypeId)
				{
					AttachmentCodeSync.ServerSetCode(itemIdentifier.SerialNumber, AttachmentsUtils.GetRandomAttachmentsCode(newType));
					source.transform.position = CS$<>8__locals1.newPos;
					base.AddResultToCombiner(source);
					return;
				}
				this.HandleOldFirearm(itemIdentifier, newType, new Action<ItemType, int>(CS$<>8__locals1.<UpgradePickup>g__SpawnAmmoPickup|0));
				PickupSyncInfo pickupSyncInfo = new PickupSyncInfo
				{
					ItemId = newType,
					Serial = ItemSerialGenerator.GenerateNext(),
					WeightKg = itemBase.Weight
				};
				ItemPickupBase itemPickupBase = InventoryExtensions.ServerCreatePickup(itemBase, new PickupSyncInfo?(pickupSyncInfo), CS$<>8__locals1.newPos, source.transform.rotation, true, null);
				source.DestroySelf();
				base.AddResultToCombiner(itemPickupBase);
				return;
			}
		}

		private void HandleOldFirearm(ItemIdentifier oldId, ItemType newType, Action<ItemType, int> spawnAmmoMethod)
		{
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (!AttachmentPreview.Get(oldId, false).TryGetModule(out primaryAmmoContainerModule, true))
			{
				return;
			}
			int totalStoredAmmo = ModulesUtils.GetTotalStoredAmmo(oldId);
			Firearm firearm;
			IPrimaryAmmoContainerModule primaryAmmoContainerModule2;
			if (AttachmentPreview.TryGet(newType, 0U, true, out firearm) && firearm.TryGetModule(out primaryAmmoContainerModule2, true))
			{
				int num;
				int num2;
				AmmoItemProcessor.ExchangeAmmo(primaryAmmoContainerModule.AmmoType, primaryAmmoContainerModule2.AmmoType, totalStoredAmmo, out num, out num2);
				spawnAmmoMethod(primaryAmmoContainerModule.AmmoType, num2);
				spawnAmmoMethod(primaryAmmoContainerModule2.AmmoType, num);
				return;
			}
			spawnAmmoMethod(primaryAmmoContainerModule.AmmoType, totalStoredAmmo);
		}

		private ItemType[] GetItems(Scp914KnobSetting setting, ItemType input)
		{
			FirearmItemProcessor.FirearmOutput[] array;
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
				return FirearmItemProcessor.DestroyResult;
			}
			if (array.Length == 0)
			{
				return new ItemType[] { input };
			}
			float value = global::UnityEngine.Random.value;
			float num = 0f;
			foreach (FirearmItemProcessor.FirearmOutput firearmOutput in array)
			{
				num += firearmOutput.Chance;
				if (num >= value)
				{
					return firearmOutput.TargetItems;
				}
			}
			return FirearmItemProcessor.DestroyResult;
		}

		[SerializeField]
		private FirearmItemProcessor.FirearmOutput[] _roughOutputs;

		[SerializeField]
		private FirearmItemProcessor.FirearmOutput[] _coarseOutputs;

		[SerializeField]
		private FirearmItemProcessor.FirearmOutput[] _oneToOneOutputs;

		[SerializeField]
		private FirearmItemProcessor.FirearmOutput[] _fineOutputs;

		[SerializeField]
		private FirearmItemProcessor.FirearmOutput[] _veryFineOutputs;

		private static readonly ItemType[] DestroyResult = new ItemType[] { ItemType.None };

		[Serializable]
		private struct FirearmOutput
		{
			[Range(0f, 1f)]
			public float Chance;

			public ItemType[] TargetItems;
		}
	}
}
