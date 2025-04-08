using System;
using PlayerRoles;
using Respawning.Announcements;
using Respawning.Config;

namespace Respawning.Waves
{
	public class ChaosMiniWave : MiniWaveBase<ChaosSpawnWave>, IAnimatedWave, IAnnouncedWave
	{
		public override Faction TargetFaction
		{
			get
			{
				return Faction.FoundationEnemy;
			}
		}

		public float AnimationDuration
		{
			get
			{
				return 13.49f;
			}
		}

		public bool IsAnimationPlaying { get; set; }

		public WaveAnnouncementBase Announcement { get; } = new ChaosMiniwaveAnnouncement();

		public override IWaveConfig Configuration { get; } = new StandardWaveConfig<ChaosMiniWave>();

		public override RoleTypeId DefaultRole { get; set; } = RoleTypeId.ChaosRifleman;

		public override RoleTypeId SpecialRole { get; set; } = RoleTypeId.ChaosMarauder;
	}
}
