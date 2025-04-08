using System;
using Utils;

namespace InventorySystem.Items.Usables.Scp330
{
	public class CandyPink : ICandy
	{
		public CandyKindID Kind
		{
			get
			{
				return CandyKindID.Pink;
			}
		}

		public float SpawnChanceWeight
		{
			get
			{
				return 0f;
			}
		}

		public void ServerApplyEffects(ReferenceHub hub)
		{
			ExplosionUtils.ServerExplode(hub, ExplosionType.PinkCandy);
		}
	}
}
