using System;
using System.Collections.Generic;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson
{
	public static class ThirdpersonItemAnimationManager
	{
		public static bool TryGetDefaultAnimation(AnimatedCharacterModel target, AnimState3p name, out AnimationClip clip)
		{
			if (ThirdpersonItemAnimationManager.CachedClips.TryGetValue(name, out clip))
			{
				return true;
			}
			clip = null;
			ThirdpersonItemAnimationManager.OverridesPuller.Clear();
			target.AnimatorOverride.GetOverrides(ThirdpersonItemAnimationManager.OverridesPuller);
			foreach (KeyValuePair<AnimationClip, AnimationClip> keyValuePair in ThirdpersonItemAnimationManager.OverridesPuller)
			{
				AnimState3p animState3p;
				if (Enum.TryParse<AnimState3p>(keyValuePair.Key.name, out animState3p))
				{
					if (animState3p == name)
					{
						clip = keyValuePair.Key;
					}
					ThirdpersonItemAnimationManager.CachedClips[animState3p] = keyValuePair.Key;
				}
			}
			return clip != null;
		}

		public static void ResetOverrides(AnimatedCharacterModel target, Dictionary<AnimationClip, AnimationClip> defaultOverrides)
		{
			ThirdpersonItemAnimationManager.OverridesPuller.Clear();
			target.AnimatorOverride.GetOverrides(ThirdpersonItemAnimationManager.OverridesPuller);
			for (int i = 0; i < ThirdpersonItemAnimationManager.OverridesPuller.Count; i++)
			{
				AnimationClip key = ThirdpersonItemAnimationManager.OverridesPuller[i].Key;
				AnimationClip animationClip;
				if (defaultOverrides != null)
				{
					defaultOverrides.TryGetValue(key, out animationClip);
				}
				else
				{
					animationClip = null;
				}
				ThirdpersonItemAnimationManager.OverridesPuller[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, animationClip);
			}
			target.AnimatorOverride.ApplyOverrides(ThirdpersonItemAnimationManager.OverridesPuller);
		}

		public static void SetAnimation(AnimatedCharacterModel target, AnimState3p name, AnimationClip clip)
		{
			AnimationClip animationClip;
			if (!ThirdpersonItemAnimationManager.TryGetDefaultAnimation(target, name, out animationClip))
			{
				return;
			}
			target.AnimatorOverride[animationClip] = clip;
		}

		public static void SetAnimation(AnimatedCharacterModel target, AnimOverrideState3pPair pair)
		{
			ThirdpersonItemAnimationManager.SetAnimation(target, pair.State, pair.Override);
		}

		private static readonly List<KeyValuePair<AnimationClip, AnimationClip>> OverridesPuller = new List<KeyValuePair<AnimationClip, AnimationClip>>();

		private static readonly Dictionary<AnimState3p, AnimationClip> CachedClips = new Dictionary<AnimState3p, AnimationClip>();
	}
}
