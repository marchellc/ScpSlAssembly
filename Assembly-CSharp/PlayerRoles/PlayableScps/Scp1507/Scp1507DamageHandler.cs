using System;
using Footprinting;
using PlayerStatsSystem;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507DamageHandler : AttackerDamageHandler
	{
		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return "Pecked by " + this.Attacker.Nickname;
			}
		}

		public Scp1507DamageHandler(Footprint attacker, float damage)
		{
			this.Attacker = attacker;
			this.Damage = damage;
		}
	}
}
