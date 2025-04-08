using System;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables
{
	public class Medkit : Consumable
	{
		protected override void OnEffectsActivated()
		{
			base.Owner.playerStats.GetModule<HealthStat>().ServerHeal(65f);
			base.Owner.playerEffectsController.UseMedicalItem(this);
		}

		private const int HpToHeal = 65;
	}
}
