using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments;

public class SpectatorSelectorCollider : MonoBehaviour, IAttachmentSelectorButton
{
	private const float DefaultColor = 0.7f;

	private const float CurrentColor = 1f;

	private const float LerpSpeed = 12f;

	[SerializeField]
	private RawImage _image;

	[SerializeField]
	private SpectatorAttachmentSelector _selector;

	private Firearm _firearm;

	private AttachmentSlot _mySlot;

	public RectTransform RectTransform => this._image.rectTransform;

	public byte ButtonId { get; set; }

	public void Setup(Texture icon, AttachmentSlot slot, Vector2? pos, Firearm fa)
	{
		if (icon != null)
		{
			Vector2 sizeDelta = new Vector2(icon.width, icon.height);
			this._firearm = fa;
			this._mySlot = slot;
			this._image.texture = icon;
			this._image.rectTransform.sizeDelta = sizeDelta;
			if (pos.HasValue)
			{
				this._image.rectTransform.localPosition = pos.Value;
			}
		}
		else
		{
			this._image.rectTransform.sizeDelta = Vector2.zero;
		}
	}

	public void Click()
	{
		this._selector.ProcessCollider(this.ButtonId);
	}

	public void Hover(bool isHovering)
	{
		if (isHovering)
		{
			this._selector.ShowStats(this.ButtonId);
		}
		else
		{
			this._selector.ShowStats(-1);
		}
	}

	public void UpdateColors(AttachmentSlot slot)
	{
		if (base.gameObject.activeSelf)
		{
			bool flag = slot == this._mySlot || (this._firearm != null && this.ButtonId < this._firearm.Attachments.Length && this._firearm.Attachments[this.ButtonId].IsEnabled);
			this._image.color = Color.Lerp(this._image.color, Color.Lerp(Color.black, Color.white, flag ? 1f : 0.7f), 12f * Time.deltaTime);
		}
	}
}
