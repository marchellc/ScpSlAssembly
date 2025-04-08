using System;
using PlayerStatsSystem;

namespace CustomPlayerEffects
{
	public interface IDamageModifierEffect
	{
		float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType);

		bool DamageModifierActive { get; }
	}
}
