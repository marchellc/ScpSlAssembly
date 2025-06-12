using System;
using InventorySystem.Items.Firearms.Attachments.Components;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments;

[Serializable]
public class AttachmentLink
{
	private bool _instanceSet;

	private Attachment _cachedInstance;

	private bool _filterSet;

	private uint _cachedFilter;

	[SerializeField]
	public int Id;

	public Attachment Instance
	{
		get
		{
			if (!this._instanceSet)
			{
				throw new InvalidOperationException("Attempting to access attachment without assigning an instance.");
			}
			return this._cachedInstance;
		}
	}

	public uint Filter
	{
		get
		{
			if (!this._filterSet)
			{
				throw new InvalidOperationException("Attempting to access attachment without assigning an item type.");
			}
			return this._cachedFilter;
		}
	}

	public void InitCache(Firearm fa)
	{
		this._cachedInstance = this.GetAttachment(fa);
		this._instanceSet = true;
		this._cachedFilter = 1u;
		for (int i = 0; i < this._cachedInstance.Index; i++)
		{
			this._cachedFilter *= 2u;
		}
		this._filterSet = true;
	}

	public void InitCache(ItemType firearmType)
	{
		this._filterSet = true;
		this.TryGetFilter(firearmType, out this._cachedFilter);
	}

	public Attachment GetAttachment(Firearm instance)
	{
		this.TryGetAttachment(instance, out var att);
		return att;
	}

	public bool TryGetAttachment(Firearm instance, out Attachment att)
	{
		return instance.TryGetAttachmentWithId(this.Id, out att);
	}

	public bool TryGetIndex(ItemType weaponType, out int index)
	{
		if (!weaponType.TryGetTemplate<Firearm>(out var item) || !this.TryGetAttachment(item, out var att))
		{
			index = -1;
			return false;
		}
		index = att.Index;
		return true;
	}

	public bool TryGetFilter(ItemType weaponType, out uint filter)
	{
		if (!this.TryGetIndex(weaponType, out var index))
		{
			filter = 0u;
			return false;
		}
		uint num = 1u;
		for (int i = 0; i < index; i++)
		{
			num *= 2;
		}
		filter = num;
		return true;
	}

	public uint GetFilter(Firearm firearm)
	{
		int index = this.GetAttachment(firearm).Index;
		uint num = 1u;
		for (int i = 0; i < index; i++)
		{
			num *= 2;
		}
		return num;
	}
}
