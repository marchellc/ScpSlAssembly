using System.Text;
using Subtitles;
using Utils.Networking;

namespace Respawning.Announcements;

public class ChaosMiniwaveAnnouncement : WaveAnnouncementBase
{
	public override void CreateAnnouncementString(StringBuilder builder)
	{
		builder.Append("ATTENTION SECURITY PERSONNEL . CHAOSINSURGENCY SPOTTED AT GATE A");
	}

	public override void SendSubtitles()
	{
		SubtitlePart subtitlePart = new SubtitlePart(SubtitleType.ChaosMiniwaveEntrance);
		new SubtitleMessage(subtitlePart).SendToAuthenticated();
	}
}
