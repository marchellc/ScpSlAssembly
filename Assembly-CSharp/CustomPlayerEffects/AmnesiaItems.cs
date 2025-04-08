using System;
using InventorySystem.Items.Firearms.Attachments;
using Mirror;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class AmnesiaItems : StatusEffectBase, IUsableItemModifierEffect, IWeaponModifierPlayerEffect, IPulseEffect
	{
		public bool ParamsActive
		{
			get
			{
				return base.IsEnabled && this._activeTime >= this._blockDelay;
			}
		}

		protected override void Update()
		{
			base.Update();
			if (base.IsEnabled)
			{
				this._activeTime += Time.deltaTime;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._activeTime = 0f;
		}

		public bool TryGetSpeed(ItemType item, out float speed)
		{
			speed = 0f;
			if (!NetworkServer.active || !this._blockedUsableItems.Contains(item) || this._activeTime < this._blockDelay)
			{
				return false;
			}
			this.ServerSendPulse();
			return true;
		}

		public bool TryGetWeaponParam(AttachmentParam param, out float val)
		{
			val = 1f;
			if (!NetworkServer.active || param != AttachmentParam.PreventReload || this._activeTime < this._blockDelay)
			{
				return false;
			}
			this.ServerSendPulse();
			return true;
		}

		public void ExecutePulse()
		{
		}

		private void ServerSendPulse()
		{
			base.Hub.playerEffectsController.ServerSendPulse<AmnesiaItems>();
		}

		private float _activeTime;

		[SerializeField]
		private ItemType[] _blockedUsableItems;

		[SerializeField]
		private float _blockDelay;
	}
}
