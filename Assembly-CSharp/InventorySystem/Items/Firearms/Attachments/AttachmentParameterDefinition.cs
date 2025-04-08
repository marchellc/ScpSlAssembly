using System;
using System.Collections.Generic;

namespace InventorySystem.Items.Firearms.Attachments
{
	public readonly struct AttachmentParameterDefinition
	{
		public float DefaultValue
		{
			get
			{
				return (float)((this.MixingMode == ParameterMixingMode.Percent) ? 1 : 0);
			}
		}

		public AttachmentParameterDefinition(ParameterMixingMode mode, float min = -3.4028235E+38f, float max = 3.4028235E+38f)
		{
			this.MixingMode = mode;
			this.MinValue = min;
			this.MaxValue = max;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static AttachmentParameterDefinition()
		{
			Dictionary<AttachmentParam, AttachmentParameterDefinition> dictionary = new Dictionary<AttachmentParam, AttachmentParameterDefinition>();
			dictionary[AttachmentParam.AdsZoomMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.AdsMouseSensitivityMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.DamageMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.PenetrationMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.FireRateMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.OverallRecoilMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, 0.2f, float.MaxValue);
			dictionary[AttachmentParam.AdsRecoilMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, 0.2f, float.MaxValue);
			dictionary[AttachmentParam.BulletInaccuracyMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.HipInaccuracyMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, 0.2f, float.MaxValue);
			dictionary[AttachmentParam.AdsInaccuracyMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, 0.1f, float.MaxValue);
			dictionary[AttachmentParam.DrawSpeedMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.GunshotLoudnessMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.MagazineCapacityModifier] = new AttachmentParameterDefinition(ParameterMixingMode.Additive, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.DrawTimeModifier] = new AttachmentParameterDefinition(ParameterMixingMode.Additive, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.ReloadTimeModifier] = new AttachmentParameterDefinition(ParameterMixingMode.Additive, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.ShotClipIdOverride] = new AttachmentParameterDefinition(ParameterMixingMode.Override, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.AdsSpeedMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.SpreadMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.SpreadPredictability] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.AmmoConsumptionMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.ReloadSpeedMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.PreventReload] = new AttachmentParameterDefinition(ParameterMixingMode.Additive, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.RunningInaccuracyMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			dictionary[AttachmentParam.DoubleActionSpeedMultiplier] = new AttachmentParameterDefinition(ParameterMixingMode.Percent, float.MinValue, float.MaxValue);
			AttachmentParameterDefinition.Definitions = dictionary;
		}

		public static readonly Dictionary<AttachmentParam, AttachmentParameterDefinition> Definitions;

		public readonly ParameterMixingMode MixingMode;

		public readonly float MinValue;

		public readonly float MaxValue;
	}
}
