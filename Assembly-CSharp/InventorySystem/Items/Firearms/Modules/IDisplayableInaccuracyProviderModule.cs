using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public interface IDisplayableInaccuracyProviderModule : IInaccuracyProviderModule
	{
		DisplayInaccuracyValues DisplayInaccuracy { get; }

		public static DisplayInaccuracyValues GetCombinedDisplayInaccuracy(Firearm firearm, bool addBulletToRest = false)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			ModuleBase[] modules = firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IDisplayableInaccuracyProviderModule displayableInaccuracyProviderModule = modules[i] as IDisplayableInaccuracyProviderModule;
				if (displayableInaccuracyProviderModule != null)
				{
					DisplayInaccuracyValues displayInaccuracy = displayableInaccuracyProviderModule.DisplayInaccuracy;
					num += displayInaccuracy.HipDeg;
					num2 += displayInaccuracy.AdsDeg;
					num3 += displayInaccuracy.RunningDeg;
					num4 += displayInaccuracy.BulletDeg;
				}
			}
			if (addBulletToRest)
			{
				num += num4;
				num2 += num4;
				num3 += num4;
			}
			return new DisplayInaccuracyValues(num, num2, num3, num4);
		}

		public static float GetDisplayAccurateRange(float dispersionDegrees, bool imperial, bool rounded)
		{
			double num = (double)(dispersionDegrees * 0.017453292f);
			double num2 = 2.0 * Math.Tan(num);
			if (num2 <= 0.0)
			{
				return 5000f;
			}
			float num3 = (float)(1.0 / num2);
			if (imperial)
			{
				num3 *= 1.0936f;
			}
			if (!rounded)
			{
				return num3;
			}
			if (num3 > 200f)
			{
				return Mathf.Round(num3 / 5f) * 5f;
			}
			if (num3 > 100f)
			{
				return Mathf.Round(num3);
			}
			return Mathf.Round(num3 * 10f) / 10f;
		}
	}
}
