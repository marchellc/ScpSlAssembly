using System;
using AudioPooling;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Flashlight
{
	public class FlashlightViewmodel : StandardAnimatedViemodel
	{
		public void PlayAnimation()
		{
			this.AnimatorSetTrigger(FlashlightViewmodel.ToggleHash);
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			this._light = base.GetComponentInChildren<Light>(true);
			FlashlightNetworkHandler.OnStatusReceived += this.OnStatusReceived;
			bool flag;
			if (FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(base.ItemId.SerialNumber, out flag))
			{
				this._light.enabled = flag;
			}
			if (!wasEquipped)
			{
				return;
			}
			this.AnimatorForceUpdate(base.SkipEquipTime, true);
			base.GetComponent<AudioSource>().mute = true;
		}

		private void OnStatusReceived(FlashlightNetworkHandler.FlashlightMessage msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			if (this._light.enabled == msg.NewState)
			{
				return;
			}
			this.PlayAnimation();
			this._light.enabled = msg.NewState;
			AudioSourcePoolManager.Play2D(msg.NewState ? FlashlightItem.Template.OnClip : FlashlightItem.Template.OffClip, 1f, MixerChannel.DefaultSfx, 1f);
		}

		private void OnDestroy()
		{
			if (!base.IsSpectator)
			{
				return;
			}
			FlashlightNetworkHandler.OnStatusReceived -= this.OnStatusReceived;
		}

		private static readonly int ToggleHash = Animator.StringToHash("Toggle");

		private Light _light;
	}
}
