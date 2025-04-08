using System;
using AudioPooling;
using InventorySystem.GUI;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ToggleableLights
{
	public abstract class ToggleableLightItemBase : ItemBase, IItemDescription, IItemNametag, ILightEmittingItem
	{
		public override float Weight
		{
			get
			{
				return 0.7f;
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public virtual bool IsEmittingLight
		{
			get
			{
				return this._isEmitting;
			}
			set
			{
				if (this._isEmitting == value)
				{
					return;
				}
				this._isEmitting = value;
				if (!this.IsLocalPlayer)
				{
					return;
				}
				this.SetLightSourceStatus(value);
				AudioSourcePoolManager.Play2D(value ? this.OnClip : this.OffClip, 1f, MixerChannel.DefaultSfx, 1f);
			}
		}

		protected abstract void SetLightSourceStatus(bool value);

		public override void OnEquipped()
		{
			this.ForceEnable();
			if (NetworkServer.active)
			{
				new FlashlightNetworkHandler.FlashlightMessage(base.ItemSerial, this.IsEmittingLight).SendToAuthenticated(0);
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
				if (Input.GetKeyDown(NewInput.GetKey(toggleKeys[i], KeyCode.None)))
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
			if (!this.IsLocalPlayer || value == this.IsEmittingLight)
			{
				return;
			}
			this.IsEmittingLight = value;
			NetworkClient.Send<FlashlightNetworkHandler.FlashlightMessage>(new FlashlightNetworkHandler.FlashlightMessage(base.ItemSerial, value), 0);
		}

		protected const float ToggleCooldownTime = 0.13f;

		protected const float EquipCooldownTime = 0.6f;

		private bool _isEmitting;

		[NonSerialized]
		public float NextAllowedTime;

		public AudioClip OnClip;

		public AudioClip OffClip;

		private static readonly ActionName[] ToggleKeys = new ActionName[]
		{
			ActionName.Shoot,
			ActionName.Zoom,
			ActionName.ToggleFlashlight
		};
	}
}
