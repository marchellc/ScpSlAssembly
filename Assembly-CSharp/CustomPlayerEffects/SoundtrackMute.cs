using System;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects
{
	public class SoundtrackMute : StatusEffectBase, ISoundtrackMutingEffect, ICustomRADisplay
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Technical;
			}
		}

		public string DisplayName { get; }

		public bool CanBeDisplayed { get; }

		public bool MuteSoundtrack
		{
			get
			{
				return base.IsEnabled;
			}
		}
	}
}
