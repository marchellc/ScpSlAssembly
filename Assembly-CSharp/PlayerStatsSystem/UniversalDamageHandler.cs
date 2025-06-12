using Mirror;

namespace PlayerStatsSystem;

public class UniversalDamageHandler : StandardDamageHandler
{
	private string _ragdollInspectText;

	private string _deathscreenText;

	private string _logsText;

	private readonly CassieAnnouncement _cassieAnnouncement;

	public readonly byte TranslationId;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => this._deathscreenText;

	public override CassieAnnouncement CassieDeathAnnouncement => this._cassieAnnouncement;

	public override string ServerLogsText => this._logsText;

	public override string ServerMetricsText
	{
		get
		{
			byte translationId = this.TranslationId;
			return translationId.ToString();
		}
	}

	public UniversalDamageHandler()
	{
		this.TranslationId = 0;
		this._cassieAnnouncement = CassieAnnouncement.Default;
	}

	public UniversalDamageHandler(float damage, DeathTranslation deathReason, CassieAnnouncement cassieAnnouncement = null)
	{
		this.Damage = damage;
		this.ApplyTranslation(deathReason);
		this.TranslationId = deathReason.Id;
		this._cassieAnnouncement = cassieAnnouncement ?? CassieAnnouncement.Default;
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte(this.TranslationId);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		if (DeathTranslations.TranslationsById.TryGetValue(reader.ReadByte(), out var value))
		{
			this.ApplyTranslation(value);
		}
	}

	private void ApplyTranslation(DeathTranslation translation)
	{
		this._ragdollInspectText = translation.RagdollTranslation;
		this._deathscreenText = translation.DeathscreenTranslation;
		this._logsText = translation.LogLabel;
	}
}
