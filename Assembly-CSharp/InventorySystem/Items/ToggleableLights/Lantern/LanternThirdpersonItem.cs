using System;
using AudioPooling;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern;

public class LanternThirdpersonItem : IdleThirdpersonItem
{
	private static LanternItem _cachedFlashlight;

	private static bool _cacheSet;

	[SerializeField]
	private Light _lightSource;

	[SerializeField]
	private ParticleSystem _particleSystem;

	private static LanternItem Template
	{
		get
		{
			if (LanternThirdpersonItem._cacheSet)
			{
				return LanternThirdpersonItem._cachedFlashlight;
			}
			if (!InventoryItemLoader.TryGetItem<LanternItem>(ItemType.Lantern, out var result))
			{
				throw new InvalidOperationException($"Item {ItemType.Lantern} is not defined!");
			}
			LanternThirdpersonItem._cachedFlashlight = result;
			LanternThirdpersonItem._cacheSet = true;
			return result;
		}
	}

	internal override void Initialize(InventorySubcontroller subctrl, ItemIdentifier id)
	{
		base.Initialize(subctrl, id);
		FlashlightNetworkHandler.OnStatusReceived += ProcessReceivedStatus;
		this.SetState(!FlashlightNetworkHandler.ReceivedStatuses.TryGetValue(id.SerialNumber, out var value) || value);
	}

	private void OnDestroy()
	{
		FlashlightNetworkHandler.OnStatusReceived -= ProcessReceivedStatus;
	}

	private void ProcessReceivedStatus(FlashlightNetworkHandler.FlashlightMessage msg)
	{
		if (msg.Serial == base.ItemId.SerialNumber)
		{
			this.SetState(msg.NewState);
		}
	}

	private void SetState(bool newState)
	{
		if (this._lightSource.enabled != newState)
		{
			this._lightSource.enabled = newState;
			if (newState)
			{
				this._particleSystem.Play();
			}
			else
			{
				this._particleSystem.Stop();
			}
			AudioSourcePoolManager.PlayOnTransform(newState ? LanternThirdpersonItem.Template.OnClip : LanternThirdpersonItem.Template.OffClip, base.transform, 3.2f);
		}
	}
}
