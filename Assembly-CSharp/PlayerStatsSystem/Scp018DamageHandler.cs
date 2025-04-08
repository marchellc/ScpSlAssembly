using System;
using Footprinting;
using InventorySystem.Items.ThrowableProjectiles;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class Scp018DamageHandler : AttackerDamageHandler
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

		public override bool IgnoreFriendlyFireDetector
		{
			get
			{
				return true;
			}
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
			this.StartVelocity = this._ballImpactVelocity * 0.5f;
			return handlerOutput;
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			base.ProcessRagdoll(ragdoll);
			DynamicRagdoll dynamicRagdoll = ragdoll as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				return;
			}
			foreach (HitboxData hitboxData in dynamicRagdoll.Hitboxes)
			{
				if (hitboxData.RelatedHitbox == HitboxType.Body)
				{
					hitboxData.Target.velocity *= 3f;
				}
			}
		}

		public Scp018DamageHandler(Scp018Projectile ball, float dmg, bool ignoreFF)
		{
			if (dmg == 0f)
			{
				return;
			}
			this._ballImpactVelocity = ball.RecreatedVelocity;
			this._serverLogsText = "SCP-018 thrown by: " + ball.PreviousOwner.Nickname;
			this.Attacker = ball.PreviousOwner;
			this.Damage = dmg;
			this.ForceFullFriendlyFire = ignoreFF;
		}

		private readonly string _deathScreenText;

		private readonly string _serverLogsText;

		private readonly string _ragdollInspectText;

		private readonly Vector3 _ballImpactVelocity;

		private const float ForceMultiplier = 0.5f;

		private const float HipMultiplier = 3f;
	}
}
