using System;
using Mirror;
using Subtitles;

namespace PlayerStatsSystem
{
	public class CustomReasonDamageHandler : StandardDamageHandler
	{
		public override float Damage { get; internal set; }

		public override DamageHandlerBase.CassieAnnouncement CassieDeathAnnouncement
		{
			get
			{
				return this._cassieAnnouncement;
			}
		}

		public override string ServerLogsText
		{
			get
			{
				return "Killed with a custom reason - " + this._deathReason;
			}
		}

		public CustomReasonDamageHandler(string customReason)
		{
			this._deathReason = customReason;
			this.Damage = -1f;
			this._cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement();
		}

		public CustomReasonDamageHandler(string customReason, float damage, string customCassieAnnouncement = "")
		{
			this._deathReason = customReason;
			this.Damage = damage;
			this._cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement();
			this._cassieAnnouncement.Announcement = customCassieAnnouncement;
			this._cassieAnnouncement.SubtitleParts = new SubtitlePart[]
			{
				new SubtitlePart(SubtitleType.Custom, new string[] { customCassieAnnouncement })
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

		private string _deathReason;

		private readonly DamageHandlerBase.CassieAnnouncement _cassieAnnouncement;
	}
}
