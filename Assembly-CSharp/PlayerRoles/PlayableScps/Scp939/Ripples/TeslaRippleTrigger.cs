using System;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class TeslaRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			TeslaGate.OnBursted += this.OnTeslaBursted;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._cooldown.Clear();
			TeslaGate.OnBursted -= this.OnTeslaBursted;
		}

		private void OnTeslaBursted(TeslaGate tg)
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			base.PlayInRange(tg.transform.position + TeslaRippleTrigger.PosOffset, 2400f, Color.red);
		}

		private void Update()
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			if (!this._cooldown.IsReady)
			{
				return;
			}
			this._cooldown.Trigger(0.699999988079071);
			foreach (TeslaGate teslaGate in TeslaGate.AllGates)
			{
				if (teslaGate.isIdling)
				{
					base.PlayInRange(teslaGate.transform.position + TeslaRippleTrigger.PosOffset, 120f, Color.red);
				}
			}
		}

		private const float CooldownDuration = 0.7f;

		private const float IdleRangeSqr = 120f;

		private const float BurstRangeSqr = 2400f;

		private static readonly Vector3 PosOffset = Vector3.up * 1.35f;

		private readonly AbilityCooldown _cooldown = new AbilityCooldown();
	}
}
