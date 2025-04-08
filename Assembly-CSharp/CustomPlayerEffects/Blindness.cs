using System;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects
{
	public class Blindness : StatusEffectBase, ICustomRADisplay, ICustomHealableEffect, IHealableEffect, IConflictableEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Negative;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		public string DisplayName
		{
			get
			{
				return "Blindness";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

		public bool IsHealable(ItemType item)
		{
			if (item == ItemType.SCP500)
			{
				byte intensity = base.Intensity;
				return intensity <= 100 && intensity > 15;
			}
			return false;
		}

		public void OnHeal(ItemType item)
		{
			base.Intensity = 15;
		}

		public bool CheckConflicts(StatusEffectBase other)
		{
			if (!(other is Flashed))
			{
				return false;
			}
			other.ServerDisable();
			return true;
		}

		private const float LerpingSpeed = 10f;

		private const byte MaxHealableIntensity = 100;

		private const byte MinIntensity = 15;
	}
}
