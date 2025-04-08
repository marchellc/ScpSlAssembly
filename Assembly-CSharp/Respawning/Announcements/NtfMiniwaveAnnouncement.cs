using System;
using System.Collections.Generic;
using System.Text;
using PlayerRoles;
using Respawning.NamingRules;
using Subtitles;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace Respawning.Announcements
{
	public class NtfMiniwaveAnnouncement : WaveAnnouncementBase
	{
		public NtfMiniwaveAnnouncement(Team team)
		{
			this._team = team;
		}

		public override void CreateAnnouncementString(StringBuilder builder)
		{
			UnitNamingRule unitNamingRule;
			if (!NamingRulesManager.TryGetNamingRule(this._team, out unitNamingRule))
			{
				return;
			}
			string lastGeneratedName = unitNamingRule.LastGeneratedName;
			string text = unitNamingRule.TranslateToCassie(lastGeneratedName);
			int num = ReferenceHub.AllHubs.Count((ReferenceHub x) => x.IsSCP(false));
			builder.Append("NINETAILEDFOX BACKUP UNIT DESIGNATED ");
			builder.Append(text);
			builder.Append(" HASENTERED ");
			if (num == 0)
			{
				builder.Append("NOSCPSLEFT");
			}
			else
			{
				builder.Append("AWAITINGRECONTAINMENT ");
				builder.Append(num);
				if (num == 1)
				{
					builder.Append(" SCPSUBJECT");
				}
				else
				{
					builder.Append(" SCPSUBJECTS");
				}
			}
			List<SubtitlePart> list = new List<SubtitlePart>
			{
				new SubtitlePart(SubtitleType.NTFMiniwaveEntrance, new string[] { lastGeneratedName })
			};
			if (num != 0)
			{
				if (num != 1)
				{
					list.Add(new SubtitlePart(SubtitleType.AwaitContainPlural, new string[] { num.ToString() }));
				}
				else
				{
					list.Add(new SubtitlePart(SubtitleType.AwaitContainSingle, null));
				}
			}
			else
			{
				list.Add(new SubtitlePart(SubtitleType.ThreatRemains, null));
			}
			new SubtitleMessage(list.ToArray()).SendToAuthenticated(0);
		}

		private readonly Team _team;
	}
}
