using Interactables;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments;

public class WorkstationSelectorCollider : InteractableCollider, IAttachmentSelectorButton
{
	private const float HighlightColor = 0.71f;

	private const float DefaultColor = 0.38f;

	private const float CurrentColor = 0.54f;

	private const float LerpSpeed = 12f;

	private const float SizeRatio = 1f;

	private const float MinColliderWidth = 40f;

	private const float MinColliderDepth = 0.2f;

	[SerializeField]
	private BoxCollider _collider;

	[SerializeField]
	private RawImage _image;

	private Firearm _firearm;

	private AttachmentSlot _mySlot;

	private bool _prevHighlighted;

	public RectTransform RectTransform => this._image.rectTransform;

	public byte ButtonId
	{
		get
		{
			return base.ColliderId;
		}
		set
		{
			base.ColliderId = value;
		}
	}

	public void Setup(Texture icon, AttachmentSlot slot, Vector2? pos, Firearm fa)
	{
		if (icon != null)
		{
			this._firearm = fa;
			this._mySlot = slot;
			this._image.texture = icon;
			this._image.rectTransform.sizeDelta = new Vector2(icon.width, icon.height);
			if (pos.HasValue)
			{
				this._image.rectTransform.localPosition = pos.Value;
			}
		}
		else
		{
			this._image.rectTransform.sizeDelta = Vector2.zero;
			this._collider.size = Vector3.zero;
		}
	}

	public void UpdateColors(AttachmentSlot slot)
	{
		if (!base.gameObject.activeSelf)
		{
			return;
		}
		Vector2 sizeDelta = this._image.rectTransform.sizeDelta;
		this._collider.size = new Vector3(Mathf.Max(sizeDelta.x * 1f, 40f), Mathf.Max(sizeDelta.y * 1f, 40f), 0.2f);
		bool flag = CenterScreenRaycast.LastRaycastHit.collider == this._collider && CenterScreenRaycast.LastRaycastHit.distance < 3.35f;
		bool flag2 = slot == this._mySlot || (this._firearm != null && base.ColliderId < this._firearm.Attachments.Length && this._firearm.Attachments[base.ColliderId].IsEnabled);
		if (this._prevHighlighted != flag && this.ButtonId < this._firearm.Attachments.Length && base.Target.TryGetComponent<AttachmentSelectorBase>(out var component))
		{
			if (flag)
			{
				component.ShowStats(this.ButtonId);
			}
			else
			{
				component.ShowStats(-1);
			}
			this._prevHighlighted = flag;
		}
		float t = (flag ? 0.71f : (flag2 ? 0.54f : 0.38f));
		this._image.color = Color.Lerp(this._image.color, Color.Lerp(Color.black, Color.white, t), 12f * Time.deltaTime);
	}
}
