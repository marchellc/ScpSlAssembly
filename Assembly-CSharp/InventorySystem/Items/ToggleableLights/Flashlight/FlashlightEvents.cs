using System;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight
{
	public class FlashlightEvents : MonoBehaviour
	{
		private void Toggle()
		{
			if (this._ivb.ParentItem == null)
			{
				return;
			}
			FlashlightItem flashlightItem = this._ivb.ParentItem.OwnerInventory.CurInstance as FlashlightItem;
			if (flashlightItem == null)
			{
				return;
			}
			flashlightItem.ClientSendRequest(!flashlightItem.IsEmittingLight);
		}

		[SerializeField]
		private ItemViewmodelBase _ivb;
	}
}
