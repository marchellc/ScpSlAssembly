using System;

namespace CustomPlayerEffects
{
	public class Concussed : StatusEffectBase, IHealableEffect
	{
		public bool IsHealable(ItemType it)
		{
			return it == ItemType.SCP500 || it == ItemType.Adrenaline || it == ItemType.Painkillers;
		}
	}
}
