using System;
using Footprinting;

namespace PlayerStatsSystem
{
	public class RecontainmentDamageHandler : AttackerDamageHandler
	{
		public RecontainmentDamageHandler(Footprint attacker)
		{
			this.Attacker = attacker;
			this.Damage = -1f;
		}

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return true;
			}
		}

		public override float Damage { get; internal set; }

		public override string ServerLogsText
		{
			get
			{
				return "Recontained by " + this.Attacker.Nickname;
			}
		}
	}
}
