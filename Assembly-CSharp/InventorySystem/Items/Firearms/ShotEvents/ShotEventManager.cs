using System;

namespace InventorySystem.Items.Firearms.ShotEvents
{
	public static class ShotEventManager
	{
		public static event ShotEventManager.Shot OnShot;

		public static void Trigger(ShotEvent ev)
		{
			ShotEventManager.Shot onShot = ShotEventManager.OnShot;
			if (onShot == null)
			{
				return;
			}
			onShot(ev);
		}

		public delegate void Shot(ShotEvent shotEvent);
	}
}
