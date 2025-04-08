using System;
using PlayerRoles.FirstPersonControl;

namespace CustomPlayerEffects
{
	public class MovementBoost : StatusEffectBase, IMovementSpeedModifier, ISpectatorDataPlayerEffect
	{
		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return (100f + (float)base.Intensity) / 100f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = string.Format("+{0}% Movement Boost", base.Intensity);
			return true;
		}
	}
}
