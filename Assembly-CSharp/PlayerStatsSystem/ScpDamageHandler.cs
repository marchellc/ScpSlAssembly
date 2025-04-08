using System;
using Footprinting;
using Mirror;

namespace PlayerStatsSystem
{
	public class ScpDamageHandler : AttackerDamageHandler
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
				return string.Concat(new string[]
				{
					"Died to SCP (",
					this.Attacker.Nickname,
					", ",
					this.Attacker.Role.ToString(),
					")"
				});
			}
		}

		public override bool AllowSelfDamage
		{
			get
			{
				return false;
			}
		}

		public ScpDamageHandler()
		{
		}

		public ScpDamageHandler(ReferenceHub attacker, float damage, DeathTranslation deathReason)
		{
			this.Attacker = new Footprint(attacker);
			this.Damage = damage;
			this._translationId = deathReason.Id;
		}

		public ScpDamageHandler(ReferenceHub attacker, DeathTranslation deathReason)
		{
			this.Attacker = new Footprint(attacker);
			this.Damage = -1f;
			this._translationId = deathReason.Id;
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte(this._translationId);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			DeathTranslation deathTranslation;
			DeathTranslations.TranslationsById.TryGetValue(reader.ReadByte(), out deathTranslation);
		}

		private string _ragdollInspectText;

		private readonly byte _translationId;
	}
}
