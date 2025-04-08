using System;
using Mirror;

namespace PlayerStatsSystem
{
	public class UniversalDamageHandler : StandardDamageHandler
	{
		public override float Damage { get; internal set; }

		public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
		{
			get
			{
				return this._cassieAnnouncement;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return this._logsText;
			}
		}

		public UniversalDamageHandler()
		{
			this.TranslationId = 0;
			this._cassieAnnouncement = DamageHandlerBase.CassieAnnouncement.Default;
		}

		public UniversalDamageHandler(float damage, DeathTranslation deathReason, DamageHandlerBase.CassieAnnouncement cassieAnnouncement = null)
		{
			this.Damage = damage;
			this.ApplyTranslation(deathReason);
			this.TranslationId = deathReason.Id;
			this._cassieAnnouncement = cassieAnnouncement ?? DamageHandlerBase.CassieAnnouncement.Default;
		}

		public override void WriteAdditionalData(NetworkWriter writer)
		{
			base.WriteAdditionalData(writer);
			writer.WriteByte(this.TranslationId);
		}

		public override void ReadAdditionalData(NetworkReader reader)
		{
			base.ReadAdditionalData(reader);
			DeathTranslation deathTranslation;
			if (DeathTranslations.TranslationsById.TryGetValue(reader.ReadByte(), out deathTranslation))
			{
				this.ApplyTranslation(deathTranslation);
			}
		}

		private void ApplyTranslation(DeathTranslation translation)
		{
			this._logsText = translation.LogLabel;
		}

		private string _ragdollInspectText;

		private string _deathscreenText;

		private string _logsText;

		private readonly DamageHandlerBase.CassieAnnouncement _cassieAnnouncement;

		public readonly byte TranslationId;
	}
}
