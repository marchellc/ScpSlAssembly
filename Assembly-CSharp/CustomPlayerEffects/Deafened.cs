using System;

namespace CustomPlayerEffects
{
	public class Deafened : StatusEffectBase, IHealableEffect
	{
		public bool IsHealable(ItemType it)
		{
			return it == ItemType.SCP500;
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		private void OnDestroy()
		{
			base.Hub.playerEffectsController.mixer.SetFloat("MasterVolumeLowpassFreq", 22000f);
		}
	}
}
