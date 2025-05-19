using System.Text;
using Subtitles;
using Utils.Networking;

namespace Respawning.Announcements;

public class ChaosWaveAnnouncement : WaveAnnouncementBase
{
	public override void CreateAnnouncementString(StringBuilder builder)
	{
		builder.Append("Security Alert . Substantial Chaos Insurgent Activity Detected . Security Personnel Proceed with Standard Protocols");
	}

	public override void SendSubtitles()
	{
		SubtitlePart subtitlePart = new SubtitlePart(SubtitleType.ChaosEntrance);
		new SubtitleMessage(subtitlePart).SendToAuthenticated();
	}
}
