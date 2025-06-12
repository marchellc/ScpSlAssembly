using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components;

public class SerializableAttachment : Attachment, IDisplayableAttachment
{
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

	public override AttachmentName Name => this._name;

	public override AttachmentSlot Slot => this._slot;

	public override float Weight => this._weight;

	public override float Length => this._length;

	public override AttachmentDescriptiveAdvantages DescriptivePros => this._extraPros;

	public override AttachmentDescriptiveDownsides DescriptiveCons => this._extraCons;

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

	public int ParentId => this._parentId;

	public Vector2 ParentOffset => this._parentOffset;

	protected virtual void Awake()
	{
		this.SetDefaultParameters();
	}

	protected void SetDefaultParameters()
	{
		this._params.ForEach(base.SetParameter);
	}

	protected virtual void Reset()
	{
		if (base.TryGetComponent<SerializableAttachment>(out var component) && !(component == this))
		{
			this._name = component._name;
			this._slot = component._slot;
			this._weight = component._weight;
			this._length = component._length;
			this._extraPros = component._extraPros;
			this._extraCons = component._extraCons;
			this._icon = component._icon;
			this._iconOffset = component._iconOffset;
			this._parentId = component._parentId;
			this._parentOffset = component._parentOffset;
			this._params = component._params;
		}
	}
}
