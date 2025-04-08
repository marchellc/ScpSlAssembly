using System;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Stained : StatusEffectBase, IStaminaModifier, IMovementSpeedModifier, IFootstepEffect
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
				return 0.8f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return true;
			}
		}

		public float ProcessFootstepOverrides(float dis)
		{
			AudioSourcePoolManager.PlayOnTransform(this._stainedFootsteps.RandomItem<AudioClip>(), base.transform, dis, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			return this._originalLoudness;
		}

		[SerializeField]
		private AudioClip[] _stainedFootsteps;

		[SerializeField]
		private float _originalLoudness;

		private const float SpeedMultiplier = 0.8f;
	}
}
