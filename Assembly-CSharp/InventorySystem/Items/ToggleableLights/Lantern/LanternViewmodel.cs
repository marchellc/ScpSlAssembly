using AudioPooling;
using InventorySystem.Items.ToggleableLights.Flashlight;

namespace InventorySystem.Items.ToggleableLights.Lantern;

public class LanternViewmodel : StandardAnimatedViemodel
{
	public LanternLightManager LanternLightManager;

	public void SetLightStatus(bool lightEnabled)
	{
		this.LanternLightManager.SetLight(lightEnabled);
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		this.LanternLightManager = base.GetComponentInChildren<LanternLightManager>(includeInactive: true);
		FlashlightNetworkHandler.OnStatusReceived += OnStatusReceived;
		this.SetLightStatus(!FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(base.ItemId.SerialNumber, out var value) || value);
		if (wasEquipped)
		{
			this.AnimatorForceUpdate(base.SkipEquipTime);
		}
	}

	private void OnStatusReceived(FlashlightNetworkHandler.FlashlightMessage msg)
	{
		if (msg.Serial == base.ItemId.SerialNumber && this.LanternLightManager.IsEnabled != msg.NewState)
		{
			this.SetLightStatus(msg.NewState);
			AudioSourcePoolManager.Play2DWithParent(msg.NewState ? FlashlightItem.Template.OnClip : FlashlightItem.Template.OffClip, base.transform);
		}
	}

	private void OnDestroy()
	{
		if (base.IsSpectator)
		{
			FlashlightNetworkHandler.OnStatusReceived -= OnStatusReceived;
		}
	}
}
