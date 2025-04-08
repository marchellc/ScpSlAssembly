using System;
using CustomPlayerEffects;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyRainbow : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Rainbow;
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
			hub.playerStats.GetModule<HealthStat>().ServerHeal(15f);
			hub.playerEffectsController.EnableEffect<Invigorated>(5f, true);
			bool flag = this._previousProcess != null;
			float num = (flag ? this._previousProcess.CurrentAmount : 0f);
			float num2 = 0f;
			if (flag)
			{
				this._previousProcess.CurrentAmount = 0f;
			}
			this._previousProcess = hub.playerStats.GetModule<AhpStat>().ServerAddProcess(num + 20f);
			this._previousProcess.SustainTime = num2 + 10f;
			hub.playerEffectsController.EnableEffect<RainbowTaste>(10f, false);
			BodyshotReduction effect = hub.playerEffectsController.GetEffect<BodyshotReduction>();
			if (effect.Intensity < 255)
			{
				BodyshotReduction bodyshotReduction = effect;
				byte intensity = bodyshotReduction.Intensity;
				bodyshotReduction.Intensity = intensity + 1;
			}
		}

		private const int HealthInstant = 15;

		private const int InvigorationDuration = 5;

		private const bool InvigorationDurationStacking = true;

		private const int AhpInstant = 20;

		private const int AhpSustainDuration = 10;

		private const bool AhpSustainDurationStacking = false;

		private const int RainbowDuration = 10;

		private const bool RainbowDurationStacking = false;

		private const bool BodyshotReductionStacking = true;

		private AhpStat.AhpProcess _previousProcess;
	}
}
