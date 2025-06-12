using Subtitles;

namespace PlayerStatsSystem;

public class WarheadDamageHandler : StandardDamageHandler
{
	private readonly string _ragdollinspectText;

	private readonly string _deathscreenText;

	public override CassieAnnouncement CassieDeathAnnouncement
	{
		get
		{
			CassieAnnouncement cassieAnnouncement = new CassieAnnouncement();
			cassieAnnouncement.Announcement = "SUCCESSFULLY TERMINATED BY ALPHA WARHEAD";
			cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
			{
				new SubtitlePart(SubtitleType.TerminatedByWarhead, (string[])null)
			};
			return cassieAnnouncement;
		}
	}

	public override float Damage { get; set; }

	public override string RagdollInspectText => this._ragdollinspectText;

	public override string DeathScreenText => this._deathscreenText;

	public override string ServerLogsText => "Died to alpha warhead.";

	public WarheadDamageHandler()
	{
		this.Damage = -1f;
		this._ragdollinspectText = DeathTranslations.Warhead.RagdollTranslation;
		this._deathscreenText = DeathTranslations.Warhead.DeathscreenTranslation;
	}
}
