using System;
using CustomPlayerEffects;
using UnityEngine;

namespace FacilitySoundtrack
{
	public abstract class SoundtrackLayerBase : MonoBehaviour
	{
		public abstract float Weight { get; }

		public abstract bool Additive { get; }

		protected bool IsPovMuted
		{
			get
			{
				ReferenceHub referenceHub;
				return ReferenceHub.TryGetPovHub(out referenceHub) && this.IsMutedForPlayer(referenceHub);
			}
		}

		public abstract void UpdateVolume(float volumeScale);

		protected bool IsMutedForPlayer(ReferenceHub hub)
		{
			StatusEffectBase[] allEffects = hub.playerEffectsController.AllEffects;
			for (int i = 0; i < allEffects.Length; i++)
			{
				ISoundtrackMutingEffect soundtrackMutingEffect = allEffects[i] as ISoundtrackMutingEffect;
				if (soundtrackMutingEffect != null && soundtrackMutingEffect.MuteSoundtrack)
				{
					return true;
				}
			}
			return false;
		}
	}
}
