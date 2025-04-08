using System;
using PlayerRoles.FirstPersonControl;

namespace CustomPlayerEffects
{
	public class Disabled : StatusEffectBase, IMovementSpeedModifier
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
				return (this.SpeedMultiplier - 1f) * RainbowTaste.CurrentMultiplier(base.Hub) + 1f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public float SpeedMultiplier = 0.8f;
	}
}
