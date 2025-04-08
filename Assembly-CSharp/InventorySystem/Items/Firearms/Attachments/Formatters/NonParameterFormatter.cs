using System;
using InventorySystem.Items.Firearms.Attachments.Components;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Formatters
{
	public static class NonParameterFormatter
	{
		private static string WeightString
		{
			get
			{
				return "\n" + TranslationReader.Get("InventoryGUI", 5, "NO_TRANSLATION") + ": ";
			}
		}

		private static string LengthString
		{
			get
			{
				return "\n" + TranslationReader.Get("InventoryGUI", 6, "NO_TRANSLATION") + ": ";
			}
		}

		public static void Format(Firearm fa, int attachmentId, out string pros, out string cons)
		{
			int num = attachmentId;
			pros = string.Empty;
			cons = string.Empty;
			Attachment attachment = fa.Attachments[attachmentId];
			for (int i = 0; i < fa.Attachments.Length; i++)
			{
				if (fa.Attachments[i].Slot == fa.Attachments[attachmentId].Slot)
				{
					num = i;
					break;
				}
			}
			pros += NonParameterFormatter.FormatFlags((int)attachment.DescriptivePros, typeof(AttachmentDescriptiveAdvantages), "AttachmentDescriptiveAdvantages");
			cons += NonParameterFormatter.FormatFlags((int)attachment.DescriptiveCons, typeof(AttachmentDescriptiveDownsides), "AttachmentDescriptiveDownsides");
			if (num == attachmentId)
			{
				return;
			}
			float num2;
			float num3;
			fa.GetDefaultLengthAndWeight(out num2, out num3);
			float num4 = (attachment.Length - fa.Attachments[num].Length + num2) / num2;
			float num5 = (attachment.Weight - fa.Attachments[num].Weight + num3) / num3;
			string text = NonParameterFormatter.LengthString + NonParameterFormatter.FormatPercent(num4);
			string text2 = NonParameterFormatter.WeightString + NonParameterFormatter.FormatPercent(num5);
			if (num4 > 1f)
			{
				cons += text;
			}
			if (num4 < 1f)
			{
				pros += text;
			}
			if (num5 > 1f)
			{
				cons += text2;
			}
			if (num5 < 1f)
			{
				pros += text2;
			}
		}

		private static string FormatPercent(float percent)
		{
			return ((percent < 1f) ? "-" : "+") + Mathf.RoundToInt(Mathf.Abs(percent - 1f) * 100f).ToString() + "%";
		}

		private static string FormatFlags(int value, Type enumType, string translationKey)
		{
			int num = 2;
			string text = string.Empty;
			for (int i = 0; i < 8; i++)
			{
				if (Enum.IsDefined(enumType, num) && (value & num) == num)
				{
					text = text + "\n" + TranslationReader.Get(translationKey, i, "NO_FLAGS_TRANSLATION");
				}
				num *= 2;
			}
			return text;
		}

		private const int FlagsIteration = 8;
	}
}
