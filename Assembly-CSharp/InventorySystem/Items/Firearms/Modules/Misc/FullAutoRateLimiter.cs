using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class FullAutoRateLimiter
	{
		public bool Ready
		{
			get
			{
				return this._remainingCooldown <= 0f;
			}
		}

		public void Update()
		{
			if (this.Ready)
			{
				this._lastWasReady = true;
				return;
			}
			this._lastWasReady = false;
			this._remainingCooldown -= Time.deltaTime;
		}

		public void Clear()
		{
			this._remainingCooldown = 0f;
			this._lastWasReady = true;
		}

		public void Trigger(float cooldown)
		{
			if (this._lastWasReady)
			{
				this._remainingCooldown = cooldown;
				return;
			}
			this._remainingCooldown += cooldown;
		}

		private bool _lastWasReady;

		private float _remainingCooldown;
	}
}
