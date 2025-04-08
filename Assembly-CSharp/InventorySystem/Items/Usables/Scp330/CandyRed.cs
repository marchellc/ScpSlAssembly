using System;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyRed : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Red;
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
			Scp330Bag.AddSimpleRegeneration(hub, 9f, 5f);
		}

		private const float RegenerationDuration = 5f;

		private const float RegenerationPerSecond = 9f;
	}
}
