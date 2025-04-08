using System;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects
{
	public class Invisible : StatusEffectBase, ISpectatorDataPlayerEffect, ICustomRADisplay, IHitmarkerPreventer
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public string DisplayName
		{
			get
			{
				return "Invisibility";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

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
}
