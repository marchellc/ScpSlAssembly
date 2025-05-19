using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects;

public class SoundtrackMute : StatusEffectBase, ISoundtrackMutingEffect, ICustomRADisplay
{
	public override EffectClassification Classification => EffectClassification.Technical;

	public string DisplayName { get; }

	public bool CanBeDisplayed { get; }

	public bool MuteSoundtrack => base.IsEnabled;
}
