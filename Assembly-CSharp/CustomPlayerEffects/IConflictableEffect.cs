using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public interface IConflictableEffect
	{
		bool CheckConflicts(StatusEffectBase other);

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StatusEffectBase.OnEnabled += delegate(StatusEffectBase newEffect)
			{
				foreach (StatusEffectBase statusEffectBase in newEffect.Hub.playerEffectsController.AllEffects)
				{
					if (statusEffectBase.IsEnabled)
					{
						IConflictableEffect conflictableEffect = statusEffectBase as IConflictableEffect;
						if (conflictableEffect != null && !(statusEffectBase == newEffect))
						{
							conflictableEffect.CheckConflicts(newEffect);
						}
					}
				}
			};
		}
	}
}
