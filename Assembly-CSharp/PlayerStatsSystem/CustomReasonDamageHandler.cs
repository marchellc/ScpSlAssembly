using Mirror;
using Subtitles;

namespace PlayerStatsSystem;

public class CustomReasonDamageHandler : StandardDamageHandler
{
	private string _deathReason;

	private readonly CassieAnnouncement _cassieAnnouncement;

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._deathReason;

	public override string DeathScreenText => this._deathReason;

	public override CassieAnnouncement CassieDeathAnnouncement => this._cassieAnnouncement;

	public override string ServerLogsText => "Killed with a custom reason - " + this._deathReason;

	public CustomReasonDamageHandler(string customReason)
	{
		this._deathReason = customReason;
		this.Damage = -1f;
		this._cassieAnnouncement = new CassieAnnouncement();
	}

	public CustomReasonDamageHandler(string customReason, float damage, string customCassieAnnouncement = "")
	{
		this._deathReason = customReason;
		this.Damage = damage;
		this._cassieAnnouncement = new CassieAnnouncement();
		this._cassieAnnouncement.Announcement = customCassieAnnouncement;
		this._cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
		{
			new SubtitlePart(SubtitleType.Custom, customCassieAnnouncement)
		};
	}

	public override void WriteAdditionalData(NetworkWriter writer)
	{
		base.WriteAdditionalData(writer);
		writer.WriteString(this._deathReason);
	}

	public override void ReadAdditionalData(NetworkReader reader)
	{
		base.ReadAdditionalData(reader);
		this._deathReason = reader.ReadString();
	}
}
