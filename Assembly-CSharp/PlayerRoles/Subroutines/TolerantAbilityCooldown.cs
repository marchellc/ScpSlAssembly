using System;
using Mirror;

namespace PlayerRoles.Subroutines
{
	public class TolerantAbilityCooldown : AbilityCooldown
	{
		public bool TolerantIsReady
		{
			get
			{
				return NetworkTime.time >= base.NextUse - this._tolerance;
			}
		}

		public TolerantAbilityCooldown(float tolerance = 0.2f)
		{
			this._tolerance = (double)tolerance;
		}

		public override void Trigger(double cooldown)
		{
			if (this.IsReady)
			{
				base.Trigger(cooldown);
				return;
			}
			base.NextUse += cooldown;
		}

		private readonly double _tolerance;
	}
}
