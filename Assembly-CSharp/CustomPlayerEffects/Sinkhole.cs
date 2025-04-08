using System;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Sinkhole : StatusEffectBase, IStaminaModifier, IMovementSpeedModifier, IFootstepEffect
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
				return 1f - this._slowAmount * 0.01f;
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

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		public float ProcessFootstepOverrides(float dis)
		{
			AudioSourcePoolManager.PlayOnTransform(this._footstepSounds.RandomItem<AudioClip>(), base.transform, dis, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			return this._originalLoudness;
		}

		[SerializeField]
		[Range(0f, 100f)]
		private float _slowAmount;

		[SerializeField]
		private AudioClip[] _footstepSounds;

		[SerializeField]
		private float _originalLoudness;
	}
}
