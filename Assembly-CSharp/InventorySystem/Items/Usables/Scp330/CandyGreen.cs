using System;
using CustomPlayerEffects;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyGreen : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Green;
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
			Scp330Bag.AddSimpleRegeneration(hub, 1.5f, 80f);
			hub.playerEffectsController.EnableEffect<Vitality>(30f, true);
		}

		private const float RegenerationDuration = 80f;

		private const float RegenerationPerSecond = 1.5f;

		private const int VitalityDuration = 30;

		private const bool VitalityDurationStacking = true;
	}
}
