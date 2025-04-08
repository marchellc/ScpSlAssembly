using System;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using PlayerRoles.FirstPersonControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserSettings;
using UserSettings.UserInterfaceSettings;

namespace InventorySystem.GUI.Descriptions
{
	public class FirearmDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			TMP_Text title = this._title;
			IItemNametag itemNametag = targetItem as IItemNametag;
			title.text = ((itemNametag != null) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
			Firearm firearm = targetItem as Firearm;
			if (firearm == null)
			{
				return;
			}
			IHitregModule hitregModule;
			if (firearm.TryGetModule(out hitregModule, true))
			{
				float displayDamage = hitregModule.DisplayDamage;
				this._damageBar.SetValue(Mathf.InverseLerp(16f, 40.5f, displayDamage), (Mathf.Round(displayDamage * 10f) / 10f).ToString(), roleColor);
				float displayPenetration = hitregModule.DisplayPenetration;
				this._penetrationBar.SetValue(displayPenetration, Mathf.Round(displayPenetration * 100f).ToString() + "%", roleColor);
			}
			else
			{
				this._damageBar.SetValue(0f, "n/a", roleColor);
				this._penetrationBar.SetValue(0f, "n/a", roleColor);
			}
			IActionModule actionModule;
			if (firearm.TryGetModule(out actionModule, true))
			{
				float displayCyclicRate = actionModule.DisplayCyclicRate;
				this._firerateBar.SetValue(Mathf.InverseLerp(2.1f, 11.5f, displayCyclicRate), Mathf.Round(displayCyclicRate * 60f).ToString(), roleColor);
			}
			else
			{
				this._firerateBar.SetValue(0f, "n/a", roleColor);
			}
			DisplayInaccuracyValues combinedDisplayInaccuracy = IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(firearm, true);
			if (UserSetting<bool>.Get<UISetting>(UISetting.InaccuracyAsDispersion))
			{
				this._hipAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.HipDeg), (Mathf.Round(combinedDisplayInaccuracy.HipDeg * 10f) / 10f).ToString() + "°", roleColor);
				this._adsAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.AdsDeg), (Mathf.Round(combinedDisplayInaccuracy.AdsDeg * 100f) / 100f).ToString() + "°", roleColor);
				this._runAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.RunningDeg), (Mathf.Round(combinedDisplayInaccuracy.RunningDeg * 10f) / 10f).ToString() + "°", roleColor);
			}
			else
			{
				bool flag = UserSetting<bool>.Get<UISetting>(UISetting.ImperialUnits);
				string text = (flag ? " yd" : " m");
				float hipAccurateRange = combinedDisplayInaccuracy.GetHipAccurateRange(flag, true);
				float adsAccurateRange = combinedDisplayInaccuracy.GetAdsAccurateRange(flag, true);
				float runningAccurateRange = combinedDisplayInaccuracy.GetRunningAccurateRange(flag, true);
				this._hipAccBar.SetValue(hipAccurateRange / 75f, hipAccurateRange.ToString() + text, roleColor);
				this._adsAccBar.SetValue(adsAccurateRange / 800f, adsAccurateRange.ToString() + text, roleColor);
				this._runAccBar.SetValue(runningAccurateRange / 75f, runningAccurateRange.ToString() + text, roleColor);
			}
			object designatedMobilityControllerClass = firearm.DesignatedMobilityControllerClass;
			IStaminaModifier staminaModifier = designatedMobilityControllerClass as IStaminaModifier;
			float num = ((staminaModifier != null) ? staminaModifier.StaminaUsageMultiplier : 1f);
			this._staminaUsageBar.SetValue(Mathf.InverseLerp(1.5f, 1f, num), "+" + Mathf.Round((num - 1f) * 100f).ToString() + "%", roleColor);
			IMovementSpeedModifier movementSpeedModifier = designatedMobilityControllerClass as IMovementSpeedModifier;
			float num2 = ((movementSpeedModifier != null) ? movementSpeedModifier.MovementSpeedMultiplier : 1f);
			this._movementSpeedBar.SetValue(Mathf.InverseLerp(0.85f, 1f, num2), "-" + Mathf.Round(Mathf.Abs(num2 - 1f) * 100f).ToString() + "%", roleColor);
			uint defaultAtt = firearm.ValidateAttachmentsCode(0U);
			firearm.GenerateIcon(this._bodyImage, this._attachmentsPool, this._attachmentIconsBorders.sizeDelta, delegate(int x)
			{
				uint num5 = 1U << x;
				if ((defaultAtt & num5) != num5)
				{
					return roleColor;
				}
				return Color.white;
			});
			int num3;
			int num4;
			firearm.GetAmmoContainerData(out num3, out num4);
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (firearm.TryGetModule(out primaryAmmoContainerModule, true))
			{
				this.SetAmmoText(primaryAmmoContainerModule.AmmoType, num3, num4);
			}
			else
			{
				this.SetAmmoText(ItemType.None, num3, num4);
			}
			this._ammoText.color = roleColor;
		}

		private void SetAmmoText(ItemType ammoType, int curAmmo, int maxAmmo)
		{
			string text = TranslationReader.Get("InventoryGUI", 0, "Ammo {0}/{1} {2}");
			string text2 = string.Empty;
			ItemBase itemBase;
			if (ammoType.TryGetTemplate(out itemBase))
			{
				IItemNametag itemNametag = itemBase as IItemNametag;
				if (itemNametag != null)
				{
					text2 = "<color=white>" + itemNametag.Name + "</color>";
				}
			}
			this._ammoText.text = string.Format("<color=white>" + text, "</color>" + curAmmo.ToString(), maxAmmo, text2);
		}

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private TextMeshProUGUI _ammoText;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _damageBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _firerateBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _penetrationBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _hipAccBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _adsAccBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _runAccBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _staminaUsageBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _movementSpeedBar;

		[SerializeField]
		private AnimationCurve _accuracyToValue;

		[SerializeField]
		private RawImage _bodyImage;

		[SerializeField]
		private RawImage[] _attachmentsPool;

		[SerializeField]
		private RectTransform _attachmentIconsBorders;

		private const string ColorStart = "<color=white>";

		private const string ColorEnd = "</color>";

		[Serializable]
		public class FirearmStatBar
		{
			public void SetValue(float normalizedValue, string text, Color col)
			{
				if (!this._defaultsSet)
				{
					string text2;
					if (TranslationReader.TryGet("InventoryGUI", (int)this._headerTranslation, out text2))
					{
						this._headerText.text = text2;
					}
					this._defaultHeight = this._targetImage.rectTransform.sizeDelta.y;
					this._defaultWidth = this._targetImage.rectTransform.sizeDelta.x;
					this._defaultsSet = true;
				}
				this._valueText.text = text;
				this._targetImage.color = col;
				this._targetImage.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(normalizedValue) * this._defaultWidth, this._defaultHeight);
			}

			[SerializeField]
			private Image _targetImage;

			[SerializeField]
			private TextMeshProUGUI _valueText;

			[SerializeField]
			private TextMeshProUGUI _headerText;

			[SerializeField]
			private InventoryGuiTranslation _headerTranslation;

			private float _defaultWidth;

			private float _defaultHeight;

			private bool _defaultsSet;
		}
	}
}
