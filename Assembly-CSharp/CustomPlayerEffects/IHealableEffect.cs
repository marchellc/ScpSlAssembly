using System;

namespace CustomPlayerEffects
{
	public interface IHealableEffect
	{
		bool IsHealable(ItemType item);
	}
}
