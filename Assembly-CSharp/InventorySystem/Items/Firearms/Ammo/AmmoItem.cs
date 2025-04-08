using System;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Ammo
{
	public class AmmoItem : ItemBase, IItemNametag, ICustomSearchCompletorItem
	{
		public override float Weight
		{
			get
			{
				float num = 0.25f;
				AmmoPickup ammoPickup = this.PickupDropModel as AmmoPickup;
				return num + ((ammoPickup != null) ? ((float)ammoPickup.SavedAmmo * 0.01f) : 0f);
			}
		}

		public string Name
		{
			get
			{
				return this._caliber;
			}
		}

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.None;
			}
		}

		public SearchCompletor GetCustomSearchCompletor(ReferenceHub hub, ItemPickupBase ipb, ItemBase ib, double disSqrt)
		{
			return new AmmoSearchCompletor(hub, ipb, ib, disSqrt);
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			AmmoPickup ammoPickup = this.PickupDropModel as AmmoPickup;
			if (ammoPickup != null)
			{
				base.OwnerInventory.ServerAddAmmo(this.ItemTypeId, (int)ammoPickup.SavedAmmo);
			}
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}

		[SerializeField]
		private string _caliber;

		public int UnitPrice;
	}
}
