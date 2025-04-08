using System;
using PlayerRoles;
using Respawning.Announcements;
using Respawning.Config;

namespace Respawning.Waves
{
	public class NtfMiniWave : MiniWaveBase<NtfSpawnWave>, IAnimatedWave, IAnnouncedWave
	{
		public override Faction TargetFaction
		{
			get
			{
				return Faction.FoundationStaff;
			}
		}

		public float AnimationDuration
		{
			get
			{
				return 17.95f;
			}
		}

		public bool IsAnimationPlaying { get; set; }

		public WaveAnnouncementBase Announcement { get; } = new NtfMiniwaveAnnouncement(Team.FoundationForces);

		public override IWaveConfig Configuration { get; } = new StandardWaveConfig<NtfMiniWave>();

		public override RoleTypeId DefaultRole { get; set; } = RoleTypeId.NtfPrivate;

		public override RoleTypeId SpecialRole { get; set; } = RoleTypeId.NtfSergeant;
	}
}
