using System;
using Footprinting;
using InventorySystem.Items.Armor;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class ExplosionDamageHandler : AttackerDamageHandler
	{
		public override float Damage { get; internal set; }

		public override Footprint Attacker { get; protected set; }

		public override bool AllowSelfDamage
		{
			get
			{
				return true;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return this._serverLogsText;
			}
		}

		public ExplosionType ExplosionType { get; private set; }

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
			this.StartVelocity += this._force * 1.3f;
			return handlerOutput;
		}

		public ExplosionDamageHandler(Footprint attacker, Vector3 force, float damage, int armorPenetration, ExplosionType explosionType)
		{
			if (armorPenetration == 0)
			{
				return;
			}
			this.Attacker = attacker;
			this.ExplosionType = explosionType;
			this._force = force;
			this._serverLogsText = DeathTranslations.Explosion.LogLabel + " caused by " + attacker.Nickname;
			BodyArmor bodyArmor;
			int num = ((attacker.Hub != null && attacker.Hub.inventory.TryGetBodyArmor(out bodyArmor)) ? bodyArmor.VestEfficacy : 0);
			this.Damage = BodyArmorUtils.ProcessDamage(num, damage, armorPenetration);
		}

		private readonly string _deathScreenText;

		private readonly string _serverLogsText;

		private readonly string _ragdollInspectText;

		private readonly Vector3 _force;

		private const float ForceMultiplier = 1.3f;
	}
}
