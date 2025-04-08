using System;
using PlayerRoles.FirstPersonControl;

namespace CustomPlayerEffects
{
	public class Slowness : StatusEffectBase, IMovementSpeedModifier, IStaminaModifier, ISpectatorDataPlayerEffect
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
				return 1f - (float)base.Intensity * 0.01f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return base.Intensity == 100;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return true;
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = string.Format("{0}% Slowness", base.Intensity);
			return true;
		}
	}
}
