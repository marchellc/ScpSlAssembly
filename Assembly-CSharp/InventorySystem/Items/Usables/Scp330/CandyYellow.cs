using System;
using CustomPlayerEffects;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyYellow : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Yellow;
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
			hub.playerStats.GetModule<StaminaStat>().ModifyAmount(0.25f);
			hub.playerEffectsController.EnableEffect<Invigorated>(8f, true);
			MovementBoost effect = hub.playerEffectsController.GetEffect<MovementBoost>();
			int num = (int)(effect.Intensity + 10);
			effect.Intensity = (byte)Mathf.Clamp(num, 0, 255);
			effect.ServerChangeDuration(8f, true);
		}

		private const int InstantStamina = 25;

		private const int InvigorationDuration = 8;

		private const bool InvigorationDurationStacking = true;

		private const int BoostDuration = 8;

		private const bool BoostDurationStacking = true;

		private const int BoostIntensity = 10;

		private const bool BoostIntensityStacking = true;
	}
}
