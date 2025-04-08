using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AnimationTriggerReloaderModule : AnimatorReloaderModuleBase
	{
		protected override void StartReloading()
		{
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Reload, false);
		}

		protected override void StartUnloading()
		{
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Unload, false);
		}
	}
}
