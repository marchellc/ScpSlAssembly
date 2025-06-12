using Footprinting;
using Mirror;

namespace PlayerStatsSystem;

public class ScpDamageHandler : AttackerDamageHandler
{
	private string _ragdollInspectText;

	private readonly byte _translationId;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollInspectText;

	public override string DeathScreenText => string.Empty;

	public override CassieAnnouncement CassieDeathAnnouncement => new CassieAnnouncement();

	public override Footprint Attacker { get; protected set; }

	public override string ServerLogsText => "Died to SCP (" + this.Attacker.Nickname + ", " + this.Attacker.Role.ToString() + ")";

	public override bool AllowSelfDamage => false;

	public ScpDamageHandler()
	{
	}

	public ScpDamageHandler(ReferenceHub attacker, float damage, DeathTranslation deathReason)
	{
		this.Attacker = new Footprint(attacker);
		this.Damage = damage;
		this._translationId = deathReason.Id;
		this._ragdollInspectText = deathReason.RagdollTranslation;
	}

	public ScpDamageHandler(ReferenceHub attacker, DeathTranslation deathReason)
	{
		this.Attacker = new Footprint(attacker);
		this.Damage = -1f;
		this._translationId = deathReason.Id;
		this._ragdollInspectText = deathReason.RagdollTranslation;
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteByte(this._translationId);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		DeathTranslations.TranslationsById.TryGetValue(reader.ReadByte(), out var _);
	}
}
