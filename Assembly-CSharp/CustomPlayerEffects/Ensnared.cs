using System;
using PlayerRoles.FirstPersonControl;

namespace CustomPlayerEffects
{
	public class Ensnared : StatusEffectBase, IMovementSpeedModifier, IStaminaModifier
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
				return 0f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return 0f;
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return true;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}
	}
}
