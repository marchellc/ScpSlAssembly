using System;

namespace CustomPlayerEffects
{
	public static class UsableItemModifierEffectExtensions
	{
		public static float GetSpeedMultiplier(this ItemType type, ReferenceHub player)
		{
			float num;
			type.TryGetSpeedMultiplier(player, out num);
			return num;
		}

		public static bool TryGetSpeedMultiplier(this ItemType type, ReferenceHub player, out float multiplier)
		{
			PlayerEffectsController playerEffectsController = player.playerEffectsController;
			bool flag = false;
			multiplier = 1f;
			for (int i = 0; i < playerEffectsController.EffectsLength; i++)
			{
				StatusEffectBase statusEffectBase = playerEffectsController.AllEffects[i];
				if (statusEffectBase.IsEnabled)
				{
					IUsableItemModifierEffect usableItemModifierEffect = statusEffectBase as IUsableItemModifierEffect;
					float num;
					if (usableItemModifierEffect != null && usableItemModifierEffect.TryGetSpeed(type, out num))
					{
						multiplier *= num;
						flag = true;
					}
				}
			}
			return flag;
		}
	}
}
