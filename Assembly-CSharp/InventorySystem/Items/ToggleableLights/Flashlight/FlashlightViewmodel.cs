using AudioPooling;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight;

public class FlashlightViewmodel : StandardAnimatedViemodel
{
	private static readonly int ToggleHash = Animator.StringToHash("Toggle");

	private Light _light;

	public void PlayAnimation()
	{
		this.AnimatorSetTrigger(FlashlightViewmodel.ToggleHash);
	}

	public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
	{
		base.InitSpectator(ply, id, wasEquipped);
		this._light = base.GetComponentInChildren<Light>(includeInactive: true);
		FlashlightNetworkHandler.OnStatusReceived += OnStatusReceived;
		if (FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(base.ItemId.SerialNumber, out var value))
		{
			this._light.enabled = value;
		}
		if (wasEquipped)
		{
			this.AnimatorForceUpdate(base.SkipEquipTime);
			base.GetComponent<AudioSource>().mute = true;
		}
	}

	private void OnStatusReceived(FlashlightNetworkHandler.FlashlightMessage msg)
	{
		if (msg.Serial == base.ItemId.SerialNumber && this._light.enabled != msg.NewState)
		{
			this.PlayAnimation();
			this._light.enabled = msg.NewState;
			AudioSourcePoolManager.Play2D(msg.NewState ? FlashlightItem.Template.OnClip : FlashlightItem.Template.OffClip);
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
