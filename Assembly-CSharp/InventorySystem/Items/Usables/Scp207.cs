using System;
using CustomPlayerEffects;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables
{
	public class Scp207 : Consumable
	{
		protected override void OnEffectsActivated()
		{
			base.Owner.playerStats.GetModule<StaminaStat>().CurValue = 1f;
			base.Owner.playerStats.GetModule<HealthStat>().ServerHeal(30f);
			Scp207 scp;
			if (!base.Owner.playerEffectsController.TryGetEffect<Scp207>(out scp))
			{
				return;
			}
			byte intensity = scp.Intensity;
			if (intensity >= 4)
			{
				return;
			}
			base.Owner.playerEffectsController.ChangeState<Scp207>(intensity + 1, 0f, false);
		}

		private const int InstantHealth = 30;

		private const byte MaxColas = 4;
	}
}
