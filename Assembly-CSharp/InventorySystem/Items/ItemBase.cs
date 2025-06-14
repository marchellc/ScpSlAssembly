using System;
using Footprinting;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Thirdperson;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items;

public abstract class ItemBase : MonoBehaviour, IIdentifierProvider
{
	public ItemType ItemTypeId;

	public ItemCategory Category;

	public ItemTierFlags TierFlags;

	public ThirdpersonItemBase ThirdpersonModel;

	public ItemViewmodelBase ViewModel;

	public Texture Icon;

	public ItemThrowSettings ThrowSettings;

	public ItemPickupBase PickupDropModel;

	public ItemIdentifier ItemId => new ItemIdentifier(this.ItemTypeId, this.ItemSerial);

	public virtual ItemDescriptionType DescriptionType => ItemDescriptionType.Default;

	public ReferenceHub Owner { get; internal set; }

	public ushort ItemSerial { get; internal set; }

	public ItemAddReason ServerAddReason { get; internal set; }

	public bool IsEquipped { get; internal set; }

	public virtual bool AllowEquip => true;

	public virtual bool AllowHolster => true;

	public virtual bool AllowDropping
	{
		get
		{
			if (!this.AllowHolster)
			{
				return !this.IsEquipped;
			}
			return true;
		}
	}

	public abstract float Weight { get; }

	internal Inventory OwnerInventory => this.Owner.inventory;

	internal virtual bool IsLocalPlayer => this.Owner.isLocalPlayer;

	internal virtual bool IsDummy => this.Owner.IsDummy;

	public static event Action<ItemBase> OnItemAdded;

	public static event Action<ItemBase> OnItemRemoved;

	protected virtual void Start()
	{
		ItemBase.OnItemAdded?.Invoke(this);
	}

	protected virtual void OnDestroy()
	{
		ItemBase.OnItemRemoved?.Invoke(this);
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
		ItemPickupBase itemPickupBase = InventoryExtensions.ServerCreatePickup(psi: new PickupSyncInfo(this.ItemTypeId, this.Weight, this.ItemSerial), inv: this.OwnerInventory, item: this, spawn: spawn);
		this.OwnerInventory.ServerRemoveItem(this.ItemSerial, itemPickupBase);
		itemPickupBase.PreviousOwner = new Footprint(this.Owner);
		return itemPickupBase;
	}
}
