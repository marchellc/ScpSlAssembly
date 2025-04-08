using System;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class BodyshotReduction : StatusEffectBase, ISpectatorDataPlayerEffect, IDamageModifierEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		private float CurrentMultiplier
		{
			get
			{
				return BodyshotReduction.Multipliers[Mathf.Min((int)base.Intensity, BodyshotReduction.Multipliers.Length - 1)];
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = string.Format("Damage Reduction (Body, -{0}%)", Mathf.Round((1f - this.CurrentMultiplier) * 1000f) / 10f);
			return base.IsEnabled;
		}

		public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
		{
			if (hitboxType == HitboxType.Body)
			{
				return this.CurrentMultiplier;
			}
			return 1f;
		}

		private static readonly float[] Multipliers = new float[] { 1f, 0.95f, 0.9f, 0.875f, 0.85f };
	}
}
