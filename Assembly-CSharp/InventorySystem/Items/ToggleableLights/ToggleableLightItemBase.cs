using System;
using AudioPooling;
using InventorySystem.GUI;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ToggleableLights;

public abstract class ToggleableLightItemBase : ItemBase, IItemDescription, IItemNametag, ILightEmittingItem
{
	protected const float ToggleCooldownTime = 0.13f;

	protected const float EquipCooldownTime = 0.6f;

	private bool _isEmitting;

	[NonSerialized]
	public float NextAllowedTime;

	public AudioClip OnClip;

	public AudioClip OffClip;

	private static readonly ActionName[] ToggleKeys = new ActionName[3]
	{
		ActionName.Shoot,
		ActionName.Zoom,
		ActionName.ToggleFlashlight
	};

	public override float Weight => 0.7f;

	public string Name => base.ItemTypeId.GetName();

	public string Description => base.ItemTypeId.GetDescription();

	public virtual bool IsEmittingLight
	{
		get
		{
			return this._isEmitting;
		}
		set
		{
			if (this._isEmitting != value)
			{
				this._isEmitting = value;
				if (this.IsLocalPlayer)
				{
					this.SetLightSourceStatus(value);
					AudioSourcePoolManager.Play2D(value ? this.OnClip : this.OffClip);
				}
			}
		}
	}

	protected abstract void SetLightSourceStatus(bool value);

	public override void OnEquipped()
	{
		this.ForceEnable();
		if (NetworkServer.active)
		{
			new FlashlightNetworkHandler.FlashlightMessage(base.ItemSerial, this.IsEmittingLight).SendToAuthenticated();
		}
	}

	private void ForceEnable()
	{
		this.NextAllowedTime = Time.timeSinceLevelLoad + 0.6f;
		this.IsEmittingLight = true;
	}

	public override void EquipUpdate()
	{
		if (!this.IsLocalPlayer || !InventoryGuiController.ItemsSafeForInteraction || Time.timeSinceLevelLoad < this.NextAllowedTime)
		{
			return;
		}
		ActionName[] toggleKeys = ToggleableLightItemBase.ToggleKeys;
		for (int i = 0; i < toggleKeys.Length; i++)
		{
			if (Input.GetKeyDown(NewInput.GetKey(toggleKeys[i])))
			{
				this.OnToggled();
			}
		}
	}

	protected virtual void OnToggled()
	{
	}

	public void ClientSendRequest(bool value)
	{
		if (this.IsLocalPlayer && value != this.IsEmittingLight)
		{
			this.IsEmittingLight = value;
			NetworkClient.Send(new FlashlightNetworkHandler.FlashlightMessage(base.ItemSerial, value));
		}
	}
}
