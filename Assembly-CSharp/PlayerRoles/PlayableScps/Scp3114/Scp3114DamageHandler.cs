using System;
using Footprinting;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114DamageHandler : AttackerDamageHandler, IRagdollInspectOverride
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
				return this.Subtype.ToString();
			}
		}

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public Scp3114DamageHandler.HandlerType Subtype { get; private set; }

		public bool StartingRagdoll { get; private set; }

		public Scp3114DamageHandler(ReferenceHub attacker, float damage, Scp3114DamageHandler.HandlerType attackType)
		{
			this.Damage = damage;
			this.Subtype = attackType;
			this.Attacker = new Footprint(attacker);
		}

		public Scp3114DamageHandler()
		{
			this.Damage = 0f;
			this.Subtype = Scp3114DamageHandler.HandlerType.Slap;
			this.Attacker = default(Footprint);
		}

		public Scp3114DamageHandler(BasicRagdoll ragdoll, bool isStarting)
		{
			this.Damage = 0f;
			this._replacedHandler = ragdoll.Info.Handler;
			if (isStarting)
			{
				this.Subtype = Scp3114DamageHandler.HandlerType.Slap;
				this.StartingRagdoll = true;
				return;
			}
			this.Subtype = Scp3114DamageHandler.HandlerType.SkinSteal;
			Scp3114DamageHandler scp3114DamageHandler = ragdoll.Info.Handler as Scp3114DamageHandler;
			this.StartingRagdoll = scp3114DamageHandler != null && scp3114DamageHandler.StartingRagdoll;
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte((byte)this.Subtype);
			writer.WriteBool(this.StartingRagdoll);
			if (this.Subtype != Scp3114DamageHandler.HandlerType.SkinSteal)
			{
				return;
			}
			writer.WriteDamageHandler(this._replacedHandler);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this.Subtype = (Scp3114DamageHandler.HandlerType)reader.ReadByte();
			this.StartingRagdoll = reader.ReadBool();
			if (this.Subtype != Scp3114DamageHandler.HandlerType.SkinSteal)
			{
				return;
			}
			this._replacedHandler = reader.ReadDamageHandler();
		}

		public override void ProcessRagdoll(BasicRagdoll ragdoll)
		{
			DynamicRagdoll dynamicRagdoll = ragdoll as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				base.ProcessRagdoll(ragdoll);
				return;
			}
			if (this.Subtype == Scp3114DamageHandler.HandlerType.SkinSteal)
			{
				DamageHandlerBase replacedHandler = this._replacedHandler;
				if (replacedHandler != null)
				{
					replacedHandler.ProcessRagdoll(ragdoll);
				}
				Scp3114RagdollToBonesConverter.ConvertExisting(dynamicRagdoll);
				return;
			}
			if (this.StartingRagdoll)
			{
				dynamicRagdoll.LinkedRigidbodies.ForEach(delegate(Rigidbody rb)
				{
					rb.velocity = Physics.gravity;
				});
				return;
			}
			base.ProcessRagdoll(ragdoll);
		}

		private DamageHandlerBase _replacedHandler;

		public enum HandlerType : byte
		{
			Slap,
			Strangulation,
			SkinSteal
		}
	}
}
