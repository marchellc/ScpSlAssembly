using System;
using System.Collections.Generic;
using Footprinting;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.Spectating;

namespace PlayerStatsSystem
{
	public class Scp049DamageHandler : ScpDamageHandler
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
				return Scp049DamageHandler.LogReasons[this.DamageSubType] + " (" + this.Attacker.Nickname + ").";
			}
		}

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public Scp049DamageHandler.AttackType DamageSubType { get; private set; }

		public Scp049DamageHandler(ReferenceHub attacker, float damage, Scp049DamageHandler.AttackType attackType)
		{
			this.Damage = damage;
			this.DamageSubType = attackType;
			this.Attacker = new Footprint(attacker);
		}

		public Scp049DamageHandler(Footprint attacker, float damage, Scp049DamageHandler.AttackType attackType)
		{
			this.Damage = damage;
			this.DamageSubType = attackType;
			this.Attacker = attacker;
		}

		public Scp049DamageHandler()
		{
		}

		public override DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply)
		{
			DamageHandlerBase.HandlerOutput handlerOutput = base.ApplyDamage(ply);
			if (!NetworkServer.active || handlerOutput != DamageHandlerBase.HandlerOutput.Death)
			{
				return handlerOutput;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp049Role scp049Role = referenceHub.roleManager.CurrentRole as Scp049Role;
				Scp049SenseAbility scp049SenseAbility;
				if (scp049Role != null && scp049Role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out scp049SenseAbility))
				{
					scp049SenseAbility.ServerProcessKilledPlayer(ply);
				}
			}
			return handlerOutput;
		}

		public override void WriteDeathScreen(NetworkWriter writer)
		{
			RoleTypeId roleTypeId = ((this.Attacker.Role != RoleTypeId.Scp0492) ? RoleTypeId.Scp049 : RoleTypeId.Scp0492);
			writer.WriteSpawnReason(SpectatorSpawnReason.KilledByPlayer);
			writer.WriteString(this.Attacker.Nickname);
			writer.WriteRoleType(roleTypeId);
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte((byte)this.DamageSubType);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			this.DamageSubType = (Scp049DamageHandler.AttackType)reader.ReadByte();
		}

		// Note: this type is marked as 'beforefieldinit'.
		static Scp049DamageHandler()
		{
			Dictionary<Scp049DamageHandler.AttackType, string> dictionary = new Dictionary<Scp049DamageHandler.AttackType, string>();
			dictionary[Scp049DamageHandler.AttackType.Instakill] = "Killed directly by SCP-049";
			dictionary[Scp049DamageHandler.AttackType.CardiacArrest] = "Died to a heart-attack forced by SCP-049";
			dictionary[Scp049DamageHandler.AttackType.Scp0492] = "Terminated by an instance of SCP-049-2";
			Scp049DamageHandler.LogReasons = dictionary;
		}

		private static readonly Dictionary<Scp049DamageHandler.AttackType, string> LogReasons;

		private readonly string _ragdollInspectText;

		public enum AttackType : byte
		{
			Instakill,
			CardiacArrest,
			Scp0492
		}
	}
}
