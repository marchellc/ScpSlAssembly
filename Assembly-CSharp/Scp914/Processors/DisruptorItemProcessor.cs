using System;
using InventorySystem;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace Scp914.Processors
{
	public class DisruptorItemProcessor : Scp914ItemProcessor
	{
		public override Scp914Result UpgradePickup(Scp914KnobSetting setting, ItemPickupBase sourcePickup)
		{
			Vector3 vector = sourcePickup.transform.position + Scp914Controller.MoveVector;
			switch (setting)
			{
			case Scp914KnobSetting.Rough:
				sourcePickup.DestroySelf();
				return this.NewPickup(sourcePickup, vector, ItemType.Flashlight);
			case Scp914KnobSetting.Coarse:
				sourcePickup.DestroySelf();
				return this.NewPickup(sourcePickup, vector, ItemType.GunE11SR);
			case Scp914KnobSetting.OneToOne:
				sourcePickup.DestroySelf();
				return this.NewPickup(sourcePickup, vector, ItemType.Jailbird);
			default:
			{
				MagazineModule magazineModule;
				AttachmentPreview.Get(ItemType.ParticleDisruptor, 0U, true).TryGetModule(out magazineModule, true);
				magazineModule.ServerSetInstanceAmmo(sourcePickup.Info.Serial, magazineModule.AmmoMax);
				sourcePickup.Position = vector;
				return new Scp914Result(sourcePickup, null, sourcePickup);
			}
			}
		}

		private Scp914Result NewPickup(ItemPickupBase sourcePickup, Vector3 pos, ItemType item)
		{
			ItemPickupBase itemPickupBase = InventoryExtensions.ServerCreatePickup(item.GetTemplate(), null, pos, true, null);
			return new Scp914Result(sourcePickup, null, itemPickupBase);
		}
	}
}
