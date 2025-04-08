using System;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyBlue : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Blue;
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
			hub.playerStats.GetModule<AhpStat>().ServerAddProcess(30f).DecayRate = 0f;
		}

		private const int AhpInstant = 30;

		private const float AhpDecay = 0f;
	}
}
