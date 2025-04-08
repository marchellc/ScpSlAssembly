using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public readonly struct DisplayInaccuracyValues
	{
		public float GetHipAccurateRange(bool imperial, bool rounded)
		{
			return IDisplayableInaccuracyProviderModule.GetDisplayAccurateRange(this.HipDeg, imperial, rounded);
		}

		public float GetAdsAccurateRange(bool imperial, bool rounded)
		{
			return IDisplayableInaccuracyProviderModule.GetDisplayAccurateRange(this.AdsDeg, imperial, rounded);
		}

		public float GetRunningAccurateRange(bool imperial, bool rounded)
		{
			return IDisplayableInaccuracyProviderModule.GetDisplayAccurateRange(this.RunningDeg, imperial, rounded);
		}

		public DisplayInaccuracyValues(float hip = 0f, float ads = 0f, float running = 0f, float bullet = 0f)
		{
			this.HipDeg = hip;
			this.AdsDeg = ads;
			this.RunningDeg = running;
			this.BulletDeg = bullet;
		}

		public readonly float HipDeg;

		public readonly float AdsDeg;

		public readonly float RunningDeg;

		public readonly float BulletDeg;
	}
}
