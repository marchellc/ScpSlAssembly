using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public abstract class AttachmentConfigWindow : MonoBehaviour
	{
		private protected AttachmentSelectorBase Selector { protected get; private set; }

		private protected Attachment Attachment { protected get; private set; }

		private void DestroySelf()
		{
			global::UnityEngine.Object.Destroy(base.gameObject);
		}

		private bool Validate()
		{
			if (this.Attachment == null)
			{
				return false;
			}
			if (this._wasActive)
			{
				if (!this.Attachment.IsEnabled)
				{
					return false;
				}
				Firearm firearm = this.Attachment.Firearm;
				if (firearm.HasOwner && firearm.OwnerInventory.CurInstance != firearm)
				{
					return false;
				}
			}
			return true;
		}

		protected virtual void OnDestroy()
		{
			AttachmentSelectorBase selector = this.Selector;
			selector.OnSummaryToggled = (Action)Delegate.Remove(selector.OnSummaryToggled, new Action(this.DestroySelf));
			Action onDestroyed = this.OnDestroyed;
			if (onDestroyed == null)
			{
				return;
			}
			onDestroyed();
		}

		protected virtual void OnDisable()
		{
			this.DestroySelf();
		}

		protected virtual void Update()
		{
			if (!this.Validate())
			{
				this.DestroySelf();
				return;
			}
			this._wasActive = this.Attachment.IsEnabled;
			this.SafeUpdate();
		}

		protected virtual void SafeUpdate()
		{
		}

		public virtual void Setup(AttachmentSelectorBase selector, Attachment attachment, RectTransform transformToFit)
		{
			this.Selector = selector;
			this.Attachment = attachment;
			AttachmentSelectorBase selector2 = this.Selector;
			selector2.OnSummaryToggled = (Action)Delegate.Combine(selector2.OnSummaryToggled, new Action(this.DestroySelf));
			this.Selector.RegisterAction(this._exitTransform, delegate(Vector2 _)
			{
				this.DestroySelf();
			});
			this.SetLayout(transformToFit);
		}

		protected virtual void SetLayout(RectTransform transformToFit)
		{
			RectTransform component = base.GetComponent<RectTransform>();
			float num = transformToFit.sizeDelta.y / component.sizeDelta.y;
			component.localPosition = transformToFit.localPosition;
			component.localRotation = Quaternion.identity;
			component.localScale = Vector3.one * num;
			component.sizeDelta = transformToFit.sizeDelta / num;
		}

		private bool _wasActive;

		[SerializeField]
		private RectTransform _exitTransform;

		public Action OnDestroyed;
	}
}
