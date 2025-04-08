using System;
using InventorySystem.Items.Firearms.Attachments.Components;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	[Serializable]
	public class AttachmentLink
	{
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
			this._cachedFilter = 1U;
			for (int i = 0; i < (int)this._cachedInstance.Index; i++)
			{
				this._cachedFilter *= 2U;
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
			Attachment attachment;
			this.TryGetAttachment(instance, out attachment);
			return attachment;
		}

		public bool TryGetAttachment(Firearm instance, out Attachment att)
		{
			return instance.TryGetAttachmentWithId(this.Id, out att);
		}

		public bool TryGetIndex(ItemType weaponType, out int index)
		{
			Firearm firearm;
			Attachment attachment;
			if (!weaponType.TryGetTemplate(out firearm) || !this.TryGetAttachment(firearm, out attachment))
			{
				index = -1;
				return false;
			}
			index = (int)attachment.Index;
			return true;
		}

		public bool TryGetFilter(ItemType weaponType, out uint filter)
		{
			int num;
			if (!this.TryGetIndex(weaponType, out num))
			{
				filter = 0U;
				return false;
			}
			uint num2 = 1U;
			for (int i = 0; i < num; i++)
			{
				num2 *= 2U;
			}
			filter = num2;
			return true;
		}

		public uint GetFilter(Firearm firearm)
		{
			int index = (int)this.GetAttachment(firearm).Index;
			uint num = 1U;
			for (int i = 0; i < index; i++)
			{
				num *= 2U;
			}
			return num;
		}

		private bool _instanceSet;

		private Attachment _cachedInstance;

		private bool _filterSet;

		private uint _cachedFilter;

		[SerializeField]
		public int Id;
	}
}
