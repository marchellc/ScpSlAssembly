using System;
using System.Collections.Generic;
using Footprinting;
using PlayerRoles.PlayableScps.Scp096;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class Scp096DamageHandler : ScpDamageHandler
	{
		public override float Damage { get; internal set; }

		public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
		{
			get
			{
				return new DamageHandlerBase.CassieAnnouncement();
			}
		}

		public override Footprint Attacker { get; protected set; }

		public override string ServerLogsText
		{
			get
			{
				return Scp096DamageHandler.LogReasons[this._attackType] + " (" + this.Attacker.Nickname + ").";
			}
		}

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public Scp096DamageHandler(Scp096Role attacker, float damage, Scp096DamageHandler.AttackType attackType)
		{
			this.Damage = damage;
			if (attacker == null)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!attacker.TryGetOwner(out referenceHub))
			{
				return;
			}
			this._attackType = attackType;
			this.Attacker = new Footprint(referenceHub);
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
			Vector3 normalized = (ply.transform.position - this.Attacker.Hub.transform.position).normalized;
			switch (this._attackType)
			{
			case Scp096DamageHandler.AttackType.GateKill:
				this.StartVelocity = normalized * 2f;
				this.StartVelocity.y = -10f;
				break;
			case Scp096DamageHandler.AttackType.SlapLeft:
			case Scp096DamageHandler.AttackType.SlapRight:
			{
				Vector3 vector = this.Attacker.Hub.PlayerCameraReference.right;
				if (this._attackType == Scp096DamageHandler.AttackType.SlapLeft)
				{
					vector *= -1f;
				}
				vector += this.Attacker.Hub.transform.forward;
				vector += Vector3.up;
				this.StartVelocity = vector * (global::UnityEngine.Random.value + 1.5f) * 3f;
				break;
			}
			case Scp096DamageHandler.AttackType.Charge:
				this.StartVelocity = normalized * 8f;
				this.StartVelocity.y = 3.5f;
				break;
			}
			return handlerOutput;
		}

		public Scp096DamageHandler()
		{
		}

		// Note: this type is marked as 'beforefieldinit'.
		static Scp096DamageHandler()
		{
			Dictionary<Scp096DamageHandler.AttackType, string> dictionary = new Dictionary<Scp096DamageHandler.AttackType, string>();
			dictionary[Scp096DamageHandler.AttackType.SlapLeft] = "Got slapped by SCP-096's left hand";
			dictionary[Scp096DamageHandler.AttackType.SlapRight] = "Got slapped by SCP-096's right hand";
			dictionary[Scp096DamageHandler.AttackType.Charge] = "Stood in a line of SCP-096's charge";
			dictionary[Scp096DamageHandler.AttackType.GateKill] = "Tried to pass through a gate being breached by SCP-096";
			Scp096DamageHandler.LogReasons = dictionary;
		}

		private static readonly Dictionary<Scp096DamageHandler.AttackType, string> LogReasons;

		private readonly string _ragdollInspectText;

		private readonly Scp096DamageHandler.AttackType _attackType;

		public enum AttackType
		{
			GateKill,
			SlapLeft,
			SlapRight,
			Charge
		}
	}
}
