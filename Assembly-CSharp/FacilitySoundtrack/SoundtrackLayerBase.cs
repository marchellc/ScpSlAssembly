using CustomPlayerEffects;
using UnityEngine;

namespace FacilitySoundtrack;

public abstract class SoundtrackLayerBase : MonoBehaviour
{
	public abstract float Weight { get; }

	public abstract bool Additive { get; }

	protected bool IsPovMuted
	{
		get
		{
			if (ReferenceHub.TryGetPovHub(out var hub))
			{
				return this.IsMutedForPlayer(hub);
			}
			return false;
		}
	}

	public abstract void UpdateVolume(float volumeScale);

	protected bool IsMutedForPlayer(ReferenceHub hub)
	{
		StatusEffectBase[] allEffects = hub.playerEffectsController.AllEffects;
		for (int i = 0; i < allEffects.Length; i++)
		{
			if (allEffects[i] is ISoundtrackMutingEffect { MuteSoundtrack: not false })
			{
				return true;
			}
		}
		return false;
	}
}
