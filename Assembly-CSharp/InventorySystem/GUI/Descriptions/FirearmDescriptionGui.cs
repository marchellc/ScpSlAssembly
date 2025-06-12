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

namespace InventorySystem.GUI.Descriptions;

public class FirearmDescriptionGui : RadialDescriptionBase
{
	[Serializable]
	public class FirearmStatBar
	{
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

		public void SetValue(float normalizedValue, string text, Color col)
		{
			if (!this._defaultsSet)
			{
				if (TranslationReader.TryGet("InventoryGUI", (int)this._headerTranslation, out var val))
				{
					this._headerText.text = val;
				}
				this._defaultHeight = this._targetImage.rectTransform.sizeDelta.y;
				this._defaultWidth = this._targetImage.rectTransform.sizeDelta.x;
				this._defaultsSet = true;
			}
			this._valueText.text = text;
			this._targetImage.color = col;
			this._targetImage.rectTransform.sizeDelta = new Vector2(Mathf.Clamp01(normalizedValue) * this._defaultWidth, this._defaultHeight);
		}
	}

	[SerializeField]
	private TextMeshProUGUI _title;

	[SerializeField]
	private TextMeshProUGUI _ammoText;

	[SerializeField]
	private FirearmStatBar _damageBar;

	[SerializeField]
	private FirearmStatBar _firerateBar;

	[SerializeField]
	private FirearmStatBar _penetrationBar;

	[SerializeField]
	private FirearmStatBar _hipAccBar;

	[SerializeField]
	private FirearmStatBar _adsAccBar;

	[SerializeField]
	private FirearmStatBar _runAccBar;

	[SerializeField]
	private FirearmStatBar _staminaUsageBar;

	[SerializeField]
	private FirearmStatBar _movementSpeedBar;

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

	public override void UpdateInfo(ItemBase targetItem, Color roleColor)
	{
		this._title.text = ((targetItem is IItemNametag itemNametag) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
		if (targetItem is Firearm firearm)
		{
			if (firearm.TryGetModule<IHitregModule>(out var module))
			{
				float displayDamage = module.DisplayDamage;
				this._damageBar.SetValue(Mathf.InverseLerp(16f, 40.5f, displayDamage), (Mathf.Round(displayDamage * 10f) / 10f).ToString(), roleColor);
				float displayPenetration = module.DisplayPenetration;
				this._penetrationBar.SetValue(displayPenetration, Mathf.Round(displayPenetration * 100f) + "%", roleColor);
			}
			else
			{
				this._damageBar.SetValue(0f, "n/a", roleColor);
				this._penetrationBar.SetValue(0f, "n/a", roleColor);
			}
			if (firearm.TryGetModule<IActionModule>(out var module2))
			{
				float displayCyclicRate = module2.DisplayCyclicRate;
				this._firerateBar.SetValue(Mathf.InverseLerp(2.1f, 11.5f, displayCyclicRate), Mathf.Round(displayCyclicRate * 60f).ToString(), roleColor);
			}
			else
			{
				this._firerateBar.SetValue(0f, "n/a", roleColor);
			}
			DisplayInaccuracyValues combinedDisplayInaccuracy = IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(firearm, addBulletToRest: true);
			if (UserSetting<bool>.Get(UISetting.InaccuracyAsDispersion))
			{
				this._hipAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.HipDeg), Mathf.Round(combinedDisplayInaccuracy.HipDeg * 10f) / 10f + "°", roleColor);
				this._adsAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.AdsDeg), Mathf.Round(combinedDisplayInaccuracy.AdsDeg * 100f) / 100f + "°", roleColor);
				this._runAccBar.SetValue(this._accuracyToValue.Evaluate(combinedDisplayInaccuracy.RunningDeg), Mathf.Round(combinedDisplayInaccuracy.RunningDeg * 10f) / 10f + "°", roleColor);
			}
			else
			{
				bool flag = UserSetting<bool>.Get(UISetting.ImperialUnits);
				string text = (flag ? " yd" : " m");
				float hipAccurateRange = combinedDisplayInaccuracy.GetHipAccurateRange(flag, rounded: true);
				float adsAccurateRange = combinedDisplayInaccuracy.GetAdsAccurateRange(flag, rounded: true);
				float runningAccurateRange = combinedDisplayInaccuracy.GetRunningAccurateRange(flag, rounded: true);
				this._hipAccBar.SetValue(hipAccurateRange / 75f, hipAccurateRange + text, roleColor);
				this._adsAccBar.SetValue(adsAccurateRange / 800f, adsAccurateRange + text, roleColor);
				this._runAccBar.SetValue(runningAccurateRange / 75f, runningAccurateRange + text, roleColor);
			}
			object designatedMobilityControllerClass = firearm.DesignatedMobilityControllerClass;
			float num = ((designatedMobilityControllerClass is IStaminaModifier staminaModifier) ? staminaModifier.StaminaUsageMultiplier : 1f);
			this._staminaUsageBar.SetValue(Mathf.InverseLerp(1.5f, 1f, num), "+" + Mathf.Round((num - 1f) * 100f) + "%", roleColor);
			float num2 = ((designatedMobilityControllerClass is IMovementSpeedModifier movementSpeedModifier) ? movementSpeedModifier.MovementSpeedMultiplier : 1f);
			this._movementSpeedBar.SetValue(Mathf.InverseLerp(0.85f, 1f, num2), "-" + Mathf.Round(Mathf.Abs(num2 - 1f) * 100f) + "%", roleColor);
			uint defaultAtt = firearm.ValidateAttachmentsCode(0u);
			firearm.GenerateIcon(this._bodyImage, this._attachmentsPool, this._attachmentIconsBorders.sizeDelta, delegate(int x)
			{
				uint num3 = (uint)(1 << x);
				return ((defaultAtt & num3) != num3) ? roleColor : Color.white;
			});
			firearm.GetAmmoContainerData(out var totalStored, out var totalMax);
			if (firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module3))
			{
				this.SetAmmoText(module3.AmmoType, totalStored, totalMax);
			}
			else
			{
				this.SetAmmoText(ItemType.None, totalStored, totalMax);
			}
			this._ammoText.color = roleColor;
		}
	}

	private void SetAmmoText(ItemType ammoType, int curAmmo, int maxAmmo)
	{
		string text = TranslationReader.Get("InventoryGUI", 0, "Ammo {0}/{1} {2}");
		string arg = string.Empty;
		if (ammoType.TryGetTemplate<ItemBase>(out var item) && item is IItemNametag itemNametag)
		{
			arg = "<color=white>" + itemNametag.Name + "</color>";
		}
		this._ammoText.text = string.Format("<color=white>" + text, "</color>" + curAmmo, maxAmmo, arg);
	}
}
