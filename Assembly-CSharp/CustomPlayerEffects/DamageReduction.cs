using System;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class DamageReduction : StatusEffectBase, ISpectatorDataPlayerEffect, IDamageModifierEffect
	{
		private float CurrentMultiplier
		{
			get
			{
				return 1f - (float)base.Intensity * 0.005f;
			}
		}

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

		public bool GetSpectatorText(out string s)
		{
			s = string.Format("Damage Reduction (All, -{0}%)", Mathf.Round((1f - this.CurrentMultiplier) * 1000f) / 10f);
			return base.IsEnabled;
		}

		public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
		{
			return this.CurrentMultiplier;
		}
	}
}
