using PlayerStatsSystem;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects;

public class Invisible : StatusEffectBase, ISpectatorDataPlayerEffect, ICustomRADisplay, IHitmarkerPreventer
{
	public override EffectClassification Classification => EffectClassification.Positive;

	public string DisplayName => "Invisibility";

	public bool CanBeDisplayed => true;

	public bool GetSpectatorText(out string s)
	{
		s = "SCP-268";
		return true;
	}

	public bool TryPreventHitmarker(AttackerDamageHandler attacker)
	{
		return true;
	}
}
