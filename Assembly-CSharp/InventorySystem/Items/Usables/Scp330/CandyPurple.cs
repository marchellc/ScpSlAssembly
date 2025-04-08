using System;
using CustomPlayerEffects;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyPurple : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Purple;
			}
		}

		public float SpawnChanceWeight
		{
			get
			{
				return 1f;
			}
		}

		public void ServerApplyEffects(ReferenceHub hub)
		{
			Scp330Bag.AddSimpleRegeneration(hub, 1.5f, 10f);
			DamageReduction effect = hub.playerEffectsController.GetEffect<DamageReduction>();
			effect.Intensity = (byte)Mathf.Max((int)effect.Intensity, 40);
			effect.ServerChangeDuration(15f, true);
		}

		private const int ReductionDuration = 15;

		private const int ReductionIntensity = 40;

		private const bool ReductionStacking = true;

		private const float RegenerationDuration = 10f;

		private const float RegenerationPerSecond = 1.5f;
	}
}
