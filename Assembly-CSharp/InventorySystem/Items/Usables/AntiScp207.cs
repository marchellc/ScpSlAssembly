using System;
using CustomPlayerEffects;

namespace InventorySystem.Items.Usables
{
	public class AntiScp207 : Consumable
	{
		protected override void OnEffectsActivated()
		{
			AntiScp207 antiScp;
			if (!base.Owner.playerEffectsController.TryGetEffect<AntiScp207>(out antiScp))
			{
				return;
			}
			byte intensity = antiScp.Intensity;
			if (intensity >= 4)
			{
				return;
			}
			base.Owner.playerEffectsController.ChangeState<AntiScp207>(intensity + 1, 0f, false);
		}

		private const byte MaxColas = 4;
	}
}
