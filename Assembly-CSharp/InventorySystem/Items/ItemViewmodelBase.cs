using System;
using UnityEngine;

namespace InventorySystem.Items;

public class ItemViewmodelBase : MonoBehaviour, IIdentifierProvider
{
	private bool _idSet;

	private ItemIdentifier _itemId;

	public virtual float ViewmodelCameraFOV => 70f;

	public ItemBase ParentItem { get; protected set; }

	public ItemIdentifier ItemId
	{
		get
		{
			if (this._idSet)
			{
				return this._itemId;
			}
			if (!this.IsLocal || this.ParentItem.ItemSerial == 0)
			{
				throw new InvalidOperationException("ItemId could not be set.");
			}
			this._idSet = true;
			this._itemId = new ItemIdentifier(this.ParentItem.ItemTypeId, this.ParentItem.ItemSerial);
			return this._itemId;
		}
	}

	public ReferenceHub Hub { get; private set; }

	public bool IsLocal { get; private set; }

	public bool IsSpectator { get; private set; }

	public static event Action<ItemViewmodelBase> OnLocallyInitialized;

	public static event Action<ItemViewmodelBase> OnSpectatorInitialized;

	public static event Action<ItemViewmodelBase> OnAnyInitialized;

	public virtual void InitLocal(ItemBase ib)
	{
		this.Hub = ib.Owner;
		this.ParentItem = ib;
		this.IsLocal = true;
		this.IsSpectator = false;
		ItemViewmodelBase.OnLocallyInitialized?.Invoke(this);
		this.InitAny();
	}

	public virtual void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		this.Hub = ply;
		this.IsLocal = false;
		this.IsSpectator = true;
		this._itemId = id;
		this._idSet = true;
		ItemViewmodelBase.OnSpectatorInitialized?.Invoke(this);
		this.InitAny();
	}

	public virtual void InitAny()
	{
		ItemViewmodelBase.OnAnyInitialized?.Invoke(this);
	}

	internal virtual void OnEquipped()
	{
	}
}
