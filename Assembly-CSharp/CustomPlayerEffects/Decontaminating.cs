using System;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using Subtitles;

namespace CustomPlayerEffects
{
	public class Decontaminating : TickingEffectBase
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			IHealthbarRole healthbarRole = base.Hub.roleManager.CurrentRole as IHealthbarRole;
			if (healthbarRole == null)
			{
				return;
			}
			float num = healthbarRole.MaxHealth / 10f;
			DamageHandlerBase.CassieAnnouncement cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement
			{
				Announcement = "LOST IN DECONTAMINATION SEQUENCE",
				SubtitleParts = new SubtitlePart[]
				{
					new SubtitlePart(SubtitleType.LostInDecontamination, null)
				}
			};
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Decontamination, cassieAnnouncement));
		}

		protected override void Enabled()
		{
			base.Enabled();
		}

		private const string CassieAnnouncement = "LOST IN DECONTAMINATION SEQUENCE";

		public float FogFadeInSpeed;

		public float FogFadeOutSpeed;
	}
}
