using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Respawning.Announcements;

public abstract class WaveAnnouncementBase
{
	private const float PostDetonationGlitchMultiplier = 2.5f;

	private const int DefaultGlitchMultiplier = 1;

	protected virtual float MinGlitch => 0.08f;

	protected virtual float MaxGlitch => 0.1f;

	protected virtual float MinJam => 0.07f;

	protected virtual float MaxJam => 0.09f;

	public abstract void CreateAnnouncementString(StringBuilder builder);

	public abstract void SendSubtitles();

	public virtual void PlayAnnouncement()
	{
		float num = (AlphaWarheadController.Detonated ? 2.5f : 1f);
		float glitchChance = Random.Range(this.MinGlitch, this.MaxGlitch) * num;
		float jamChance = Random.Range(this.MinJam, this.MaxJam) * num;
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		this.CreateAnnouncementString(stringBuilder);
		this.SendSubtitles();
		string tts = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		NineTailedFoxAnnouncer.singleton.ServerOnlyAddGlitchyPhrase(tts, glitchChance, jamChance);
	}
}
