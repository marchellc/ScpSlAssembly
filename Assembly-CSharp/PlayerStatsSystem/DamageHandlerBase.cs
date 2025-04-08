using System;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using Subtitles;

namespace PlayerStatsSystem
{
	public abstract class DamageHandlerBase
	{
		public abstract string ServerLogsText { get; }

		public abstract string ServerMetricsText { get; }

		public abstract DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement { get; }

		public virtual void WriteDeathScreen(NetworkWriter writer)
		{
			writer.WriteSpawnReason(SpectatorSpawnReason.Other);
			writer.WriteDamageHandler(this);
		}

		public virtual void WriteAdditionalData(NetworkWriter writer)
		{
		}

		public virtual void ReadAdditionalData(NetworkReader reader)
		{
		}

		public virtual void ProcessRagdoll(BasicRagdoll ragdoll)
		{
		}

		public abstract DamageHandlerBase.HandlerOutput ApplyDamage(ReferenceHub ply);

		public class CassieAnnouncement
		{
			public static readonly DamageHandlerBase.CassieAnnouncement Default = new DamageHandlerBase.CassieAnnouncement
			{
				Announcement = "SUCCESSFULLY TERMINATED . TERMINATION CAUSE UNSPECIFIED",
				SubtitleParts = new SubtitlePart[]
				{
					new SubtitlePart(SubtitleType.TerminationCauseUnspecified, null)
				}
			};

			public string Announcement;

			public SubtitlePart[] SubtitleParts;
		}

		public enum HandlerOutput : byte
		{
			Nothing,
			Damaged,
			Death
		}
	}
}
