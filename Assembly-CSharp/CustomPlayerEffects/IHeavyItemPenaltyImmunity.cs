using System;

namespace CustomPlayerEffects
{
	public interface IHeavyItemPenaltyImmunity
	{
		public static bool IsImmune(ReferenceHub hub)
		{
			foreach (StatusEffectBase statusEffectBase in hub.playerEffectsController.AllEffects)
			{
				if (statusEffectBase is IHeavyItemPenaltyImmunity && statusEffectBase.IsEnabled)
				{
					return true;
				}
			}
			return false;
		}
	}
}
