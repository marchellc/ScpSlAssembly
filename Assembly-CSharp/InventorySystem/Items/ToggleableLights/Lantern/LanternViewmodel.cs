using System;
using AudioPooling;
using InventorySystem.Items.ToggleableLights.Flashlight;

namespace InventorySystem.Items.ToggleableLights.Lantern
{
	public class LanternViewmodel : StandardAnimatedViemodel
	{
		public void SetLightStatus(bool lightEnabled)
		{
			this.LanternLightManager.SetLight(lightEnabled);
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			this.LanternLightManager = base.GetComponentInChildren<LanternLightManager>(true);
			FlashlightNetworkHandler.OnStatusReceived += this.OnStatusReceived;
			bool flag;
			this.SetLightStatus(!FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(base.ItemId.SerialNumber, out flag) || flag);
			if (!wasEquipped)
			{
				return;
			}
			this.AnimatorForceUpdate(base.SkipEquipTime, true);
		}

		private void OnStatusReceived(FlashlightNetworkHandler.FlashlightMessage msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			if (this.LanternLightManager.IsEnabled == msg.NewState)
			{
				return;
			}
			this.SetLightStatus(msg.NewState);
			AudioSourcePoolManager.Play2DWithParent(msg.NewState ? FlashlightItem.Template.OnClip : FlashlightItem.Template.OffClip, base.transform, 1f, MixerChannel.DefaultSfx, 1f);
		}

		private void OnDestroy()
		{
			if (!base.IsSpectator)
			{
				return;
			}
			FlashlightNetworkHandler.OnStatusReceived -= this.OnStatusReceived;
		}

		public LanternLightManager LanternLightManager;
	}
}
