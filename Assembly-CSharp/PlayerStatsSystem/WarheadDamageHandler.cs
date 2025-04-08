using System;
using Subtitles;

namespace PlayerStatsSystem
{
	public class WarheadDamageHandler : StandardDamageHandler
	{
		public WarheadDamageHandler()
		{
			this.Damage = -1f;
		}

		public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
		{
			get
			{
				return new DamageHandlerBase.CassieAnnouncement
				{
					Announcement = "SUCCESSFULLY TERMINATED BY ALPHA WARHEAD",
					SubtitleParts = new SubtitlePart[]
					{
						new SubtitlePart(SubtitleType.TerminatedByWarhead, null)
					}
				};
			}
		}

		public override float Damage { get; internal set; }

		public override string ServerLogsText
		{
			get
			{
				return "Died to alpha warhead.";
			}
		}
	}
}
