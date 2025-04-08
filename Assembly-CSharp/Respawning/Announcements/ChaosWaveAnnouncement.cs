using System;
using System.Text;
using Subtitles;
using Utils.Networking;

namespace Respawning.Announcements
{
	public class ChaosWaveAnnouncement : WaveAnnouncementBase
	{
		public override void CreateAnnouncementString(StringBuilder builder)
		{
			builder.Append("Security Alert . Substantial Chaos Insurgent Activity Detected. Security Personnel Proceed with Standard Protocols");
			SubtitlePart subtitlePart = new SubtitlePart(SubtitleType.ChaosEntrance, Array.Empty<string>());
			new SubtitleMessage(new SubtitlePart[] { subtitlePart }).SendToAuthenticated(0);
		}
	}
}
