using System;
using System.Text;
using Subtitles;
using Utils.Networking;

namespace Respawning.Announcements
{
	public class ChaosMiniwaveAnnouncement : WaveAnnouncementBase
	{
		public override void CreateAnnouncementString(StringBuilder builder)
		{
			builder.Append("ATTENTION SECURITY PERSONNEL . CHAOSINSURGENCY SPOTTED AT GATE A");
			SubtitlePart subtitlePart = new SubtitlePart(SubtitleType.ChaosMiniwaveEntrance, Array.Empty<string>());
			new SubtitleMessage(new SubtitlePart[] { subtitlePart }).SendToAuthenticated(0);
		}
	}
}
