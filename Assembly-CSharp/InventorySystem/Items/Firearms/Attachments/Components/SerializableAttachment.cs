using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class SerializableAttachment : Attachment, IDisplayableAttachment
	{
		public override AttachmentName Name
		{
			get
			{
				return this._name;
			}
		}

		public override AttachmentSlot Slot
		{
			get
			{
				return this._slot;
			}
		}

		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public override float Length
		{
			get
			{
				return this._length;
			}
		}

		public override AttachmentDescriptiveAdvantages DescriptivePros
		{
			get
			{
				return this._extraPros;
			}
		}

		public override AttachmentDescriptiveDownsides DescriptiveCons
		{
			get
			{
				return this._extraCons;
			}
		}

		public Texture2D Icon
		{
			get
			{
				return this._icon;
			}
			set
			{
				this._icon = value;
			}
		}

		public Vector2 IconOffset
		{
			get
			{
				return this._iconOffset;
			}
			set
			{
				this._iconOffset = value;
			}
		}

		public int ParentId
		{
			get
			{
				return this._parentId;
			}
		}

		public Vector2 ParentOffset
		{
			get
			{
				return this._parentOffset;
			}
		}

		protected virtual void Awake()
		{
			this.SetDefaultParameters();
		}

		protected void SetDefaultParameters()
		{
			this._params.ForEach(new Action<AttachmentParameterValuePair>(base.SetParameter));
		}

		protected virtual void Reset()
		{
			SerializableAttachment serializableAttachment;
			if (!base.TryGetComponent<SerializableAttachment>(out serializableAttachment) || serializableAttachment == this)
			{
				return;
			}
			this._name = serializableAttachment._name;
			this._slot = serializableAttachment._slot;
			this._weight = serializableAttachment._weight;
			this._length = serializableAttachment._length;
			this._extraPros = serializableAttachment._extraPros;
			this._extraCons = serializableAttachment._extraCons;
			this._icon = serializableAttachment._icon;
			this._iconOffset = serializableAttachment._iconOffset;
			this._parentId = serializableAttachment._parentId;
			this._parentOffset = serializableAttachment._parentOffset;
			this._params = serializableAttachment._params;
		}

		[SerializeField]
		private AttachmentName _name;

		[SerializeField]
		private AttachmentSlot _slot;

		[Space]
		[SerializeField]
		private float _weight;

		[SerializeField]
		private float _length;

		[Space]
		[SerializeField]
		private AttachmentDescriptiveAdvantages _extraPros;

		[SerializeField]
		private AttachmentDescriptiveDownsides _extraCons;

		[Space]
		[SerializeField]
		private Texture2D _icon;

		[SerializeField]
		private Vector2 _iconOffset;

		[SerializeField]
		private int _parentId;

		[SerializeField]
		private Vector2 _parentOffset;

		[Space]
		[SerializeField]
		private AttachmentParameterValuePair[] _params;
	}
}
