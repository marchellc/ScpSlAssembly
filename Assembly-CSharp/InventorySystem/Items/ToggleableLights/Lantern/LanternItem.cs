using System;
using InventorySystem.Items.Pickups;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern
{
	public class LanternItem : ToggleableLightItemBase
	{
		public override float Weight
		{
			get
			{
				return 0.7f;
			}
		}

		protected override void OnToggled()
		{
			this.SetLightSourceStatus(!this.IsEmittingLight);
			this.NextAllowedTime = Time.timeSinceLevelLoad + 0.13f + 0.25f;
			base.ClientSendRequest(!this.IsEmittingLight);
		}

		protected override void SetLightSourceStatus(bool value)
		{
			this._lanternViewmodel.SetLightStatus(value);
		}

		public override void OnAdded(ItemPickupBase pickup)
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			this._lanternViewmodel = this.ViewModel as LanternViewmodel;
		}

		private const float LanternCooldownTime = 0.25f;

		private LanternViewmodel _lanternViewmodel;
	}
}
