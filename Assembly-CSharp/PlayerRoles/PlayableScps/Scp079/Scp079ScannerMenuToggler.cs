using System;
using System.Text;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Map;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ScannerMenuToggler : Scp079ToggleMenuAbilityBase<Scp079ScannerMenuToggler>, IScp079LevelUpNotifier
	{
		public bool IsUnlocked
		{
			get
			{
				return base.TierManager.AccessTierIndex >= this._minimalTierIndex;
			}
		}

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079BreachScanner;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return this.IsUnlocked && Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen;
			}
		}

		protected override Scp079HudTranslation OpenTranslation
		{
			get
			{
				return Scp079HudTranslation.OpenBreachScanner;
			}
		}

		protected override Scp079HudTranslation CloseTranslation
		{
			get
			{
				return Scp079HudTranslation.CloseBreachScanner;
			}
		}

		public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
		{
			if (newLevel != this._minimalTierIndex)
			{
				return false;
			}
			sb.Append(Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.BreachScannerAvailable));
			return true;
		}

		protected override void Update()
		{
			base.Update();
			Scp079ToggleMenuAbilityBase<Scp079ScannerMenuToggler>.IsOpen &= this.IsVisible;
		}

		[SerializeField]
		private int _minimalTierIndex;
	}
}
