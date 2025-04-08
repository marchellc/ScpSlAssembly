using System;
using InventorySystem;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace Scp914.Processors
{
	public class MicroHidItemProcessor : StandardItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			MicroHIDPickup microHIDPickup = sourcePickup as MicroHIDPickup;
			if (microHIDPickup == null)
			{
				return base.UpgradePickup(setting, sourcePickup);
			}
			ushort serialNumber = microHIDPickup.ItemId.SerialNumber;
			bool broken = BrokenSyncModule.GetBroken(serialNumber);
			MicroHIDItem template = microHIDPickup.ItemId.TypeId.GetTemplate<MicroHIDItem>();
			Vector3 vector = microHIDPickup.Position + Scp914Controller.MoveVector;
			switch (setting)
			{
			case Scp914KnobSetting.Coarse:
				if (broken)
				{
					template.EnergyManager.ServerSetEnergy(serialNumber, global::UnityEngine.Random.value);
					goto IL_00B6;
				}
				template.BrokenSync.ServerSetBroken(serialNumber, true);
				goto IL_00B6;
			case Scp914KnobSetting.OneToOne:
				goto IL_00AD;
			case Scp914KnobSetting.Fine:
				break;
			case Scp914KnobSetting.VeryFine:
				if (broken)
				{
					template.BrokenSync.ServerSetBroken(serialNumber, false);
				}
				break;
			default:
				goto IL_00AD;
			}
			template.EnergyManager.ServerSetEnergy(serialNumber, 1f);
			goto IL_00B6;
			IL_00AD:
			return base.UpgradePickup(setting, sourcePickup);
			IL_00B6:
			microHIDPickup.Position = vector;
			return new Scp914Result(microHIDPickup, null, sourcePickup);
		}
	}
}
