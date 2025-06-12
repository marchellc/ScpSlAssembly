using InventorySystem.Items.Pickups;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern;

public class LanternItem : ToggleableLightItemBase
{
	private const float LanternCooldownTime = 0.25f;

	private LanternViewmodel _lanternViewmodel;

	public override float Weight => 0.7f;

	protected override void OnToggled()
	{
		this.SetLightSourceStatus(!this.IsEmittingLight);
		base.NextAllowedTime = Time.timeSinceLevelLoad + 0.13f + 0.25f;
		base.ClientSendRequest(!this.IsEmittingLight);
	}

	protected override void SetLightSourceStatus(bool value)
	{
		this._lanternViewmodel.SetLightStatus(value);
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		if (this.IsLocalPlayer)
		{
			this._lanternViewmodel = base.ViewModel as LanternViewmodel;
		}
	}
}
