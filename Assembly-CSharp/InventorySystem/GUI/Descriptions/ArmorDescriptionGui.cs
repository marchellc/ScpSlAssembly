using System;
using System.Text;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using NorthwoodLib.Pools;
using TMPro;
using UnityEngine;

namespace InventorySystem.GUI.Descriptions
{
	public class ArmorDescriptionGui : RadialDescriptionBase
	{
		public override void UpdateInfo(ItemBase targetItem, Color roleColor)
		{
			TMP_Text title = this._title;
			IItemNametag itemNametag = targetItem as IItemNametag;
			title.text = ((itemNametag != null) ? itemNametag.Name : targetItem.ItemTypeId.ToString());
			this._itemLimits.color = roleColor;
			this._ammoLimits.color = roleColor;
			BodyArmor bodyArmor = targetItem as BodyArmor;
			if (bodyArmor == null)
			{
				return;
			}
			this._helmetBar.SetValue((float)bodyArmor.HelmetEfficacy / 100f, bodyArmor.HelmetEfficacy.ToString() + "%", roleColor);
			this._vestBar.SetValue((float)bodyArmor.VestEfficacy / 100f, bodyArmor.VestEfficacy.ToString() + "%", roleColor);
			float staminaUsageMultiplier = bodyArmor.StaminaUsageMultiplier;
			float movementSpeedMultiplier = bodyArmor.MovementSpeedMultiplier;
			this._staminaBar.SetValue(staminaUsageMultiplier - 1f, "+" + Mathf.Round((staminaUsageMultiplier - 1f) * 100f).ToString() + "%", roleColor);
			this._movementBar.SetValue(movementSpeedMultiplier, "-" + Mathf.Round((1f - movementSpeedMultiplier) * 100f).ToString() + "%", roleColor);
			string text = TranslationReader.Get("InventoryGUI", 14, "total");
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			string text2;
			if (TranslationReader.TryGet("InventoryGUI", 12, out text2))
			{
				stringBuilder.Append("<size=11><color=white>");
				stringBuilder.Append(text2);
				stringBuilder.Append("</color></size>");
				foreach (BodyArmor.ArmorCategoryLimitModifier armorCategoryLimitModifier in bodyArmor.CategoryLimits)
				{
					string text3 = TranslationReader.Get("Categories", (int)armorCategoryLimitModifier.Category, armorCategoryLimitModifier.Category.ToString());
					this.AddRecord(stringBuilder, (int)armorCategoryLimitModifier.Limit, text3, (int)InventoryLimits.GetCategoryLimit(bodyArmor, armorCategoryLimitModifier.Category), text);
				}
			}
			StringBuilder stringBuilder2 = StringBuilderPool.Shared.Rent();
			string text4;
			if (TranslationReader.TryGet("InventoryGUI", 13, out text4))
			{
				stringBuilder2.Append("<size=11><color=white>");
				stringBuilder2.Append(text4);
				stringBuilder2.Append("</color></size>");
				foreach (BodyArmor.ArmorAmmoLimit armorAmmoLimit in bodyArmor.AmmoLimits)
				{
					ItemBase itemBase;
					if (InventoryItemLoader.AvailableItems.TryGetValue(armorAmmoLimit.AmmoType, out itemBase))
					{
						IItemNametag itemNametag2 = itemBase as IItemNametag;
						if (itemNametag2 != null)
						{
							this.AddRecord(stringBuilder2, (int)armorAmmoLimit.Limit, itemNametag2.Name, (int)InventoryLimits.GetAmmoLimit(bodyArmor, armorAmmoLimit.AmmoType), text);
						}
					}
				}
			}
			this._itemLimits.text = stringBuilder.ToString();
			this._ammoLimits.text = stringBuilder2.ToString();
			StringBuilderPool.Shared.Return(stringBuilder);
			StringBuilderPool.Shared.Return(stringBuilder2);
		}

		private void AddRecord(StringBuilder sb, int relativeLimit, string label, int total, string totalWord)
		{
			sb.Append("\n+");
			sb.Append(relativeLimit);
			sb.Append(" <color=white>");
			sb.Append(label);
			sb.Append("</color> (");
			sb.AppendFormat(totalWord, total);
			sb.Append(")");
		}

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private TextMeshProUGUI _itemLimits;

		[SerializeField]
		private TextMeshProUGUI _ammoLimits;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _helmetBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _vestBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _staminaBar;

		[SerializeField]
		private FirearmDescriptionGui.FirearmStatBar _movementBar;
	}
}
