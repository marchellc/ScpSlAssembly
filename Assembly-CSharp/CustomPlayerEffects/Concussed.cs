namespace CustomPlayerEffects;

public class Concussed : StatusEffectBase, IHealableEffect
{
	public bool IsHealable(ItemType it)
	{
		if (it != ItemType.SCP500 && it != ItemType.Adrenaline)
		{
			return it == ItemType.Painkillers;
		}
		return true;
	}
}
