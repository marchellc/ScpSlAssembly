using System;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Keycards
{
	public class KeycardThirdpersonItem : IdleThirdpersonItem
	{
		internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
		{
			base.Initialize(subcontroller, id);
			if (!this._eventSet)
			{
				KeycardItem.OnKeycardUsed += this.OnKeycardUsed;
				this._eventSet = true;
			}
			base.SetAnim(AnimState3p.Override1, this._useClip);
			if (this._targetRenderer == null)
			{
				return;
			}
			KeycardItem keycardItem;
			if (!InventoryItemLoader.TryGetItem<KeycardItem>(id.TypeId, out keycardItem))
			{
				return;
			}
			KeycardPickup keycardPickup = keycardItem.PickupDropModel as KeycardPickup;
			if (keycardPickup == null)
			{
				return;
			}
			this._targetRenderer.sharedMaterial = keycardPickup.GetMaterialFromKeycardId(id.TypeId);
		}

		private void OnKeycardUsed(ushort serial)
		{
			if (base.ItemId.SerialNumber != serial)
			{
				return;
			}
			base.OverrideBlend = 1f;
			base.ReplayOverrideBlend(true);
		}

		private void OnDestroy()
		{
			if (this._eventSet)
			{
				KeycardItem.OnKeycardUsed -= this.OnKeycardUsed;
			}
		}

		[SerializeField]
		private Renderer _targetRenderer;

		[SerializeField]
		private AnimationClip _useClip;

		private bool _eventSet;
	}
}
