using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Formatters
{
	public class StandardParameterFormatter : IAttachmentsParameterFormatter
	{
		public float DefaultValue
		{
			get
			{
				return (float)(this._isMultiplier ? 1 : 0);
			}
		}

		public StandardParameterFormatter(bool moreIsBetter, bool isMultiplier = true, bool formatAsPercent = true, string suffix = null)
		{
			this._isMultiplier = isMultiplier;
			this._moreIsBetter = moreIsBetter;
			this._formatAsPrecent = formatAsPercent;
			this._suffix = suffix;
		}

		public bool FormatParameter(AttachmentParam param, Firearm firearm, int attId, float statsValue, out string formattedText, out bool isGood)
		{
			formattedText = null;
			isGood = false;
			float num = this.DefaultValue;
			int i = 0;
			while (i < firearm.Attachments.Length)
			{
				if (attId >= 0 && firearm.Attachments[attId].Slot == firearm.Attachments[i].Slot)
				{
					float num2;
					if (firearm.Attachments[i].TryGetDisplayValue(param, out num2))
					{
						num = num2;
						break;
					}
					break;
				}
				else
				{
					i++;
				}
			}
			if (num == statsValue)
			{
				return false;
			}
			isGood = (this._moreIsBetter ? (statsValue > num) : (statsValue < num));
			string text = ((statsValue > num) ? "+" : "-");
			if (this._formatAsPrecent)
			{
				float num3 = ((num == 0f) ? (statsValue - num) : (1f - statsValue / num));
				formattedText = text + (Mathf.Round(Mathf.Abs(num3) * 1000f) / 10f).ToString() + "%";
			}
			else
			{
				formattedText = text + (Mathf.Round(Mathf.Abs(statsValue - num) * 10f) / 10f).ToString();
			}
			if (!string.IsNullOrEmpty(this._suffix))
			{
				formattedText += this._suffix;
			}
			return true;
		}

		private readonly bool _isMultiplier;

		private readonly bool _moreIsBetter;

		private readonly bool _formatAsPrecent;

		private readonly string _suffix;
	}
}
