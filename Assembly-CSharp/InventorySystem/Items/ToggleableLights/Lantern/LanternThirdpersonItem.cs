using System;
using AudioPooling;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern
{
	public class LanternThirdpersonItem : IdleThirdpersonItem
	{
		private static LanternItem Template
		{
			get
			{
				if (LanternThirdpersonItem._cacheSet)
				{
					return LanternThirdpersonItem._cachedFlashlight;
				}
				LanternItem lanternItem;
				if (!InventoryItemLoader.TryGetItem<LanternItem>(ItemType.Lantern, out lanternItem))
				{
					throw new InvalidOperationException(string.Format("Item {0} is not defined!", ItemType.Lantern));
				}
				LanternThirdpersonItem._cachedFlashlight = lanternItem;
				LanternThirdpersonItem._cacheSet = true;
				return lanternItem;
			}
		}

		internal override void Initialize(InventorySubcontroller subctrl, ItemIdentifier id)
		{
			base.Initialize(subctrl, id);
			FlashlightNetworkHandler.OnStatusReceived += this.ProcessReceivedStatus;
			bool flag;
			this.SetState(!FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(id.SerialNumber, out flag) || flag);
		}

		private void OnDestroy()
		{
			FlashlightNetworkHandler.OnStatusReceived -= this.ProcessReceivedStatus;
		}

		private void ProcessReceivedStatus(FlashlightNetworkHandler.FlashlightMessage msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.SetState(msg.NewState);
		}

		private void SetState(bool newState)
		{
			if (this._lightSource.enabled == newState)
			{
				return;
			}
			this._lightSource.enabled = newState;
			if (newState)
			{
				this._particleSystem.Play();
			}
			else
			{
				this._particleSystem.Stop();
			}
			AudioSourcePoolManager.PlayOnTransform(newState ? LanternThirdpersonItem.Template.OnClip : LanternThirdpersonItem.Template.OffClip, base.transform, 3.2f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		private static LanternItem _cachedFlashlight;

		private static bool _cacheSet;

		[SerializeField]
		private Light _lightSource;

		[SerializeField]
		private ParticleSystem _particleSystem;
	}
}
