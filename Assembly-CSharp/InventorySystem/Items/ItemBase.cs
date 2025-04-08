using System;
using Footprinting;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Thirdperson;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items
{
	public abstract class ItemBase : MonoBehaviour, IIdentifierProvider
	{
		public ItemIdentifier ItemId
		{
			get
			{
				return new ItemIdentifier(this.ItemTypeId, this.ItemSerial);
			}
		}

		public virtual ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Default;
			}
		}

		public ReferenceHub Owner { get; internal set; }

		public ushort ItemSerial { get; internal set; }

		public ItemAddReason ServerAddReason { get; internal set; }

		public bool IsEquipped { get; internal set; }

		public virtual bool AllowEquip
		{
			get
			{
				return true;
			}
		}

		public virtual bool AllowHolster
		{
			get
			{
				return true;
			}
		}

		public virtual bool AllowDropping
		{
			get
			{
				return this.AllowHolster || !this.IsEquipped;
			}
		}

		public abstract float Weight { get; }

		internal Inventory OwnerInventory
		{
			get
			{
				return this.Owner.inventory;
			}
		}

		internal virtual bool IsLocalPlayer
		{
			get
			{
				return this.Owner.isLocalPlayer;
			}
		}

		public virtual void OnEquipped()
		{
		}

		public virtual void EquipUpdate()
		{
		}

		public virtual void AlwaysUpdate()
		{
		}

		public virtual void OnHolstered()
		{
		}

		public virtual void OnAdded(ItemPickupBase pickup)
		{
		}

		public virtual void OnRemoved(ItemPickupBase pickup)
		{
		}

		public virtual void OnHolsterRequestSent()
		{
		}

		internal virtual void OnTemplateReloaded(bool wasEverLoaded)
		{
		}

		public virtual ItemPickupBase ServerDropItem(bool spawn)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerDropItem can only be executed on the server.");
			}
			if (this.PickupDropModel == null)
			{
				Debug.LogError("No pickup drop model set. Could not drop the item.");
				return null;
			}
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(this.ItemTypeId, this.Weight, this.ItemSerial, false);
			ItemPickupBase itemPickupBase = this.OwnerInventory.ServerCreatePickup(this, new PickupSyncInfo?(pickupSyncInfo), spawn, null);
			this.OwnerInventory.ServerRemoveItem(this.ItemSerial, itemPickupBase);
			itemPickupBase.PreviousOwner = new Footprint(this.Owner);
			return itemPickupBase;
		}

		public ItemType ItemTypeId;

		public ItemCategory Category;

		public ItemTierFlags TierFlags;

		public ThirdpersonItemBase ThirdpersonModel;

		public ItemViewmodelBase ViewModel;

		public Texture Icon;

		public ItemThrowSettings ThrowSettings;

		public ItemPickupBase PickupDropModel;
	}
}
