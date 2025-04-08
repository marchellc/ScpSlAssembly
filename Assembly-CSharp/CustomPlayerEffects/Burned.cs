using System;
using PlayerStatsSystem;

namespace CustomPlayerEffects
{
	public class Burned : StatusEffectBase, IHealableEffect, IDamageModifierEffect
	{
		public float DamageMultiplier
		{
			get
			{
				return 0.25f * RainbowTaste.CurrentMultiplier(base.Hub) + 1f;
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool IsHealable(ItemType it)
		{
			return it == ItemType.Medkit || it == ItemType.SCP500;
		}

		public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
		{
			return this.DamageMultiplier;
		}

		private const float BaseDamageMultiplier = 1.25f;
	}
}
