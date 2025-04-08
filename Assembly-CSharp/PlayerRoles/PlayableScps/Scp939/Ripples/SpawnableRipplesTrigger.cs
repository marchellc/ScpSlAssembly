using System;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class SpawnableRipplesTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			SpawnableRipple.OnSpawned += this.OnSpawned;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			SpawnableRipple.OnSpawned -= this.OnSpawned;
		}

		private void OnSpawned(SpawnableRipple sr)
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			base.PlayInRange(sr.transform.position, sr.Range, Color.red);
		}
	}
}
