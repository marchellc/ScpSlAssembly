using System.Collections.Generic;
using System.Text;
using PlayerRoles;
using Respawning.NamingRules;
using Subtitles;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace Respawning.Announcements;

public class NtfMiniwaveAnnouncement : WaveAnnouncementBase
{
	private readonly Team _team;

	private int ScpsLeft => ReferenceHub.AllHubs.Count((ReferenceHub x) => x.IsSCP(includeZombies: false));

	public NtfMiniwaveAnnouncement(Team team)
	{
		_team = team;
	}

	public override void CreateAnnouncementString(StringBuilder builder)
	{
		if (!NamingRulesManager.TryGetNamingRule(_team, out var rule))
		{
			return;
		}
		string lastGeneratedName = rule.LastGeneratedName;
		int scpsLeft = ScpsLeft;
		string value = rule.TranslateToCassie(lastGeneratedName);
		builder.Append("NINETAILEDFOX BACKUP UNIT DESIGNATED ");
		builder.Append(value);
		builder.Append(" HASENTERED ");
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
		if (NamingRulesManager.TryGetNamingRule(_team, out var rule))
		{
			List<SubtitlePart> list = new List<SubtitlePart>();
			list.Add(new SubtitlePart(SubtitleType.NTFMiniwaveEntrance, rule.LastGeneratedName));
			List<SubtitlePart> list2 = list;
			int scpsLeft = ScpsLeft;
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
