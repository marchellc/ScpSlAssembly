using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Ammo;

public class AmmoItem : ItemBase, IItemNametag
{
	[SerializeField]
	private string _caliber;

	public int UnitPrice;

	public override float Weight => 0.25f + ((base.PickupDropModel is AmmoPickup ammoPickup) ? ((float)(int)ammoPickup.SavedAmmo * 0.01f) : 0f);

	public string Name => this._caliber;

	public override ItemDescriptionType DescriptionType => ItemDescriptionType.None;

	public override void OnAdded(ItemPickupBase pickup)
	{
		if (NetworkServer.active)
		{
			if (base.PickupDropModel is AmmoPickup ammoPickup)
			{
				base.OwnerInventory.ServerAddAmmo(base.ItemTypeId, ammoPickup.SavedAmmo);
			}
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}
	}
}
