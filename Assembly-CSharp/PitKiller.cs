using System;
using CustomPlayerEffects;
using Mirror;
using UnityEngine;

public class PitKiller : MonoBehaviour
{
	public event Action<ReferenceHub> OnEffectApplied;

	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHub(other.transform.root.gameObject, out referenceHub))
		{
			return;
		}
		if (!PitDeath.ValidatePlayer(referenceHub))
		{
			return;
		}
		PitDeath effect = referenceHub.playerEffectsController.GetEffect<PitDeath>();
		if (effect.IsEnabled)
		{
			return;
		}
		Action<ReferenceHub> onEffectApplied = this.OnEffectApplied;
		if (onEffectApplied != null)
		{
			onEffectApplied(referenceHub);
		}
		effect.IsEnabled = true;
	}
}
