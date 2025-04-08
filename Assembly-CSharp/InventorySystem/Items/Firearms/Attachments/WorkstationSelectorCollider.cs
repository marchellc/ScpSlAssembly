using System;
using Interactables;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class WorkstationSelectorCollider : InteractableCollider, IAttachmentSelectorButton
	{
		public RectTransform RectTransform
		{
			get
			{
				return this._image.rectTransform;
			}
		}

		public byte ButtonId
		{
			get
			{
				return this.ColliderId;
			}
			set
			{
				this.ColliderId = value;
			}
		}

		public void Setup(Texture icon, AttachmentSlot slot, Vector2? pos, Firearm fa)
		{
			if (icon != null)
			{
				this._firearm = fa;
				this._mySlot = slot;
				this._image.texture = icon;
				this._image.rectTransform.sizeDelta = new Vector2((float)icon.width, (float)icon.height);
				if (pos != null)
				{
					this._image.rectTransform.localPosition = pos.Value;
					return;
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
			bool flag = InteractionCoordinator.LastRaycastHit.collider == this._collider && InteractionCoordinator.LastRaycastHit.distance < 3.35f;
			bool flag2 = slot == this._mySlot || (this._firearm != null && (int)this.ColliderId < this._firearm.Attachments.Length && this._firearm.Attachments[(int)this.ColliderId].IsEnabled);
			AttachmentSelectorBase attachmentSelectorBase;
			if (this._prevHighlighted != flag && (int)this.ButtonId < this._firearm.Attachments.Length && this.Target.TryGetComponent<AttachmentSelectorBase>(out attachmentSelectorBase))
			{
				if (flag)
				{
					attachmentSelectorBase.ShowStats((int)this.ButtonId);
				}
				else
				{
					attachmentSelectorBase.ShowStats(-1);
				}
				this._prevHighlighted = flag;
			}
			float num = (flag ? 0.71f : (flag2 ? 0.54f : 0.38f));
			this._image.color = Color.Lerp(this._image.color, Color.Lerp(Color.black, Color.white, num), 12f * Time.deltaTime);
		}

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
	}
}
