using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using Subtitles;

namespace CustomPlayerEffects;

public class Decontaminating : TickingEffectBase
{
	private const string CassieAnnouncement = "LOST IN DECONTAMINATION SEQUENCE";

	public float FogFadeInSpeed;

	public float FogFadeOutSpeed;

	public override bool AllowEnabling => true;

	protected override void OnTick()
	{
		if (NetworkServer.active && base.Hub.roleManager.CurrentRole is IHealthbarRole healthbarRole)
		{
			float damage = healthbarRole.MaxHealth / 10f;
			DamageHandlerBase.CassieAnnouncement cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement();
			cassieAnnouncement.Announcement = "LOST IN DECONTAMINATION SEQUENCE";
			cassieAnnouncement.SubtitleParts = new SubtitlePart[1]
			{
				new SubtitlePart(SubtitleType.LostInDecontamination, (string[])null)
			};
			DamageHandlerBase.CassieAnnouncement cassieAnnouncement2 = cassieAnnouncement;
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Decontamination, cassieAnnouncement2));
		}
	}

	protected override void Enabled()
	{
		base.Enabled();
	}
}
