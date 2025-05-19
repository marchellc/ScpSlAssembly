using CustomPlayerEffects;

namespace InventorySystem.Items.Usables;

public class AntiScp207 : Consumable
{
	private const byte MaxColas = 4;

	protected override void OnEffectsActivated()
	{
		if (base.Owner.playerEffectsController.TryGetEffect<CustomPlayerEffects.AntiScp207>(out var playerEffect))
		{
			byte intensity = playerEffect.Intensity;
			if (intensity < 4)
			{
				base.Owner.playerEffectsController.ChangeState<CustomPlayerEffects.AntiScp207>(++intensity);
			}
		}
	}
}
