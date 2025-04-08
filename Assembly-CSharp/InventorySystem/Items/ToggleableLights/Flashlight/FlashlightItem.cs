using System;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight
{
	public class FlashlightItem : ToggleableLightItemBase
	{
		public override float Weight
		{
			get
			{
				return 0.7f;
			}
		}

		protected override void SetLightSourceStatus(bool value)
		{
			this._lightSource.enabled = value;
		}

		public static FlashlightItem Template
		{
			get
			{
				if (FlashlightItem._cacheSet)
				{
					return FlashlightItem._cachedFlashlight;
				}
				FlashlightItem flashlightItem;
				if (!InventoryItemLoader.TryGetItem<FlashlightItem>(ItemType.Flashlight, out flashlightItem))
				{
					throw new InvalidOperationException(string.Format("Item {0} is not defined!", ItemType.Flashlight));
				}
				FlashlightItem._cachedFlashlight = flashlightItem;
				FlashlightItem._cacheSet = true;
				return flashlightItem;
			}
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			this._lightSource = this.ViewModel.GetComponentInChildren<Light>(true);
		}

		protected override void OnToggled()
		{
			FlashlightViewmodel flashlightViewmodel = this.ViewModel as FlashlightViewmodel;
			if (flashlightViewmodel != null)
			{
				flashlightViewmodel.PlayAnimation();
			}
			this.NextAllowedTime = Time.timeSinceLevelLoad + 0.13f;
		}

		private Light _lightSource;

		private static FlashlightItem _cachedFlashlight;

		private static bool _cacheSet;
	}
}
