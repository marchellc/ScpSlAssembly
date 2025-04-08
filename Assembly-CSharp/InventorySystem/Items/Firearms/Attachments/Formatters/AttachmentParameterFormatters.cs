using System;
using System.Collections.Generic;

namespace InventorySystem.Items.Firearms.Attachments.Formatters
{
	public static class AttachmentParameterFormatters
	{
		// Note: this type is marked as 'beforefieldinit'.
		static AttachmentParameterFormatters()
		{
			Dictionary<AttachmentParam, IAttachmentsParameterFormatter> dictionary = new Dictionary<AttachmentParam, IAttachmentsParameterFormatter>();
			dictionary[AttachmentParam.AdsZoomMultiplier] = new ZoomParameterFormatter();
			dictionary[AttachmentParam.DamageMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.PenetrationMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.FireRateMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.OverallRecoilMultiplier] = new StandardParameterFormatter(false, true, true, null);
			dictionary[AttachmentParam.AdsRecoilMultiplier] = new StandardParameterFormatter(false, true, true, null);
			dictionary[AttachmentParam.BulletInaccuracyMultiplier] = new InaccuracyParameterFormatter();
			dictionary[AttachmentParam.HipInaccuracyMultiplier] = new InaccuracyParameterFormatter();
			dictionary[AttachmentParam.AdsInaccuracyMultiplier] = new InaccuracyParameterFormatter();
			dictionary[AttachmentParam.GunshotLoudnessMultiplier] = new StandardParameterFormatter(false, true, true, null);
			dictionary[AttachmentParam.MagazineCapacityModifier] = new StandardParameterFormatter(true, false, false, null);
			dictionary[AttachmentParam.DrawTimeModifier] = new DrawParameterFormatter();
			dictionary[AttachmentParam.DrawSpeedMultiplier] = new DrawParameterFormatter();
			dictionary[AttachmentParam.ReloadTimeModifier] = new StandardParameterFormatter(false, false, false, "s");
			dictionary[AttachmentParam.AdsSpeedMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.SpreadMultiplier] = new StandardParameterFormatter(false, true, true, null);
			dictionary[AttachmentParam.SpreadPredictability] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.AmmoConsumptionMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.ReloadSpeedMultiplier] = new StandardParameterFormatter(true, true, true, null);
			dictionary[AttachmentParam.RunningInaccuracyMultiplier] = new StandardParameterFormatter(false, true, true, null);
			dictionary[AttachmentParam.DoubleActionSpeedMultiplier] = new StandardParameterFormatter(true, true, true, null);
			AttachmentParameterFormatters.Formatters = dictionary;
		}

		public static readonly Dictionary<AttachmentParam, IAttachmentsParameterFormatter> Formatters;
	}
}
