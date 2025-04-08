using System;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079TierGui : Scp079BarBaseGui
	{
		protected override string Text
		{
			get
			{
				return this._cachedText;
			}
		}

		protected override float FillAmount
		{
			get
			{
				return this._cachedFill;
			}
		}

		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			this._tierFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.AccessTier);
			this._expFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.Experience);
			this._maxTierText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.MaxTierReached);
			this._levelUpNotification = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.AccessTierUnlocked);
			role.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out this._tierManager);
			Scp079TierManager tierManager = this._tierManager;
			tierManager.OnExpChanged = (Action)Delegate.Combine(tierManager.OnExpChanged, new Action(this.SetDirty));
			Scp079TierManager tierManager2 = this._tierManager;
			tierManager2.OnLevelledUp = (Action)Delegate.Combine(tierManager2.OnLevelledUp, new Action(this.OnLevelledUp));
			this._uiDirty = true;
		}

		private void OnDestroy()
		{
			Scp079TierManager tierManager = this._tierManager;
			tierManager.OnExpChanged = (Action)Delegate.Remove(tierManager.OnExpChanged, new Action(this.SetDirty));
			Scp079TierManager tierManager2 = this._tierManager;
			tierManager2.OnLevelledUp = (Action)Delegate.Remove(tierManager2.OnLevelledUp, new Action(this.OnLevelledUp));
		}

		protected override void Update()
		{
			base.Update();
			if (!this._uiDirty)
			{
				return;
			}
			this._textTier.text = string.Format(this._tierFormat, this._tierManager.AccessTierLevel);
			if (this._tierManager.NextLevelThreshold <= 0)
			{
				this._cachedFill = 1f;
				this._cachedText = this._maxTierText;
			}
			else
			{
				this._cachedFill = (float)this._tierManager.RelativeExp / (float)this._tierManager.NextLevelThreshold;
				this._cachedText = string.Format(this._expFormat, this._tierManager.RelativeExp, this._tierManager.NextLevelThreshold);
			}
			this._uiDirty = false;
		}

		private void SetDirty()
		{
			this._uiDirty = true;
		}

		private void OnLevelledUp()
		{
			this.SetDirty();
			bool flag = false;
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.AppendFormat(this._levelUpNotification, this._tierManager.AccessTierLevel);
			SubroutineBase[] allSubroutines = base.Role.SubroutineModule.AllSubroutines;
			for (int i = 0; i < allSubroutines.Length; i++)
			{
				IScp079LevelUpNotifier scp079LevelUpNotifier = allSubroutines[i] as IScp079LevelUpNotifier;
				if (scp079LevelUpNotifier != null)
				{
					if (!flag)
					{
						stringBuilder.Append("\n  - ");
					}
					flag = !scp079LevelUpNotifier.WriteLevelUpNotification(stringBuilder, this._tierManager.AccessTierIndex);
				}
			}
			string text = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
			if (flag)
			{
				text = text.Remove(text.Length - "\n  - ".Length);
			}
			Scp079NotificationManager.AddNotification(new Scp079AccentedNotification(text, "#00a2ff", '$'));
		}

		[SerializeField]
		private TextMeshProUGUI _textTier;

		private bool _uiDirty;

		private Scp079TierManager _tierManager;

		private string _tierFormat;

		private string _expFormat;

		private string _maxTierText;

		private string _levelUpNotification;

		private string _cachedText;

		private float _cachedFill;

		public const string NewLineFormat = "\n  - ";
	}
}
