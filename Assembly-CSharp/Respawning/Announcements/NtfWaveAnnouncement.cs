using System.Collections.Generic;
using System.Text;
using MapGeneration.Holidays;
using PlayerRoles;
using Respawning.NamingRules;
using Subtitles;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace Respawning.Announcements;

public class NtfWaveAnnouncement : WaveAnnouncementBase
{
	private readonly Team _team;

	private int ScpsLeft => ReferenceHub.AllHubs.Count((ReferenceHub x) => x.IsSCP(includeZombies: false));

	public NtfWaveAnnouncement(Team team)
	{
		this._team = team;
	}

	public override void CreateAnnouncementString(StringBuilder builder)
	{
		if (!NamingRulesManager.TryGetNamingRule(this._team, out var rule))
		{
			return;
		}
		string lastGeneratedName = rule.LastGeneratedName;
		int scpsLeft = this.ScpsLeft;
		string value = rule.TranslateToCassie(lastGeneratedName);
		if (HolidayUtils.IsHolidayActive(HolidayType.Christmas))
		{
			builder.Append("XMAS_EPSILON11 ");
			builder.Append(value);
			builder.Append(" XMAS_HASENTERED ");
			builder.Append(scpsLeft);
			builder.Append(" XMAS_SCPSUBJECTS");
			return;
		}
		builder.Append("MTFUNIT EPSILON 11 DESIGNATED ");
		builder.Append(value);
		builder.Append(" HASENTERED ALLREMAINING ");
		if (scpsLeft == 0)
		{
			builder.Append("NOSCPSLEFT");
			return;
		}
		builder.Append("AWAITINGRECONTAINMENT ");
		builder.Append(scpsLeft);
		if (scpsLeft == 1)
		{
			builder.Append(" SCPSUBJECT");
		}
		else
		{
			builder.Append(" SCPSUBJECTS");
		}
	}

	public override void SendSubtitles()
	{
		if (NamingRulesManager.TryGetNamingRule(this._team, out var rule))
		{
			List<SubtitlePart> list = new List<SubtitlePart>();
			list.Add(new SubtitlePart(SubtitleType.NTFEntrance, rule.LastGeneratedName));
			List<SubtitlePart> list2 = list;
			int scpsLeft = this.ScpsLeft;
			switch (scpsLeft)
			{
			case 0:
				list2.Add(new SubtitlePart(SubtitleType.ThreatRemains, (string[])null));
				break;
			case 1:
				list2.Add(new SubtitlePart(SubtitleType.AwaitContainSingle, (string[])null));
				break;
			default:
				list2.Add(new SubtitlePart(SubtitleType.AwaitContainPlural, scpsLeft.ToString()));
				break;
			}
			new SubtitleMessage(list2.ToArray()).SendToAuthenticated();
		}
	}
}
