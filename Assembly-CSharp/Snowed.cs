using System;
using CustomPlayerEffects;
using MapGeneration.Holidays;

public class Snowed : StatusEffectBase, IHolidayEffect
{
	public HolidayType[] TargetHolidays { get; } = new HolidayType[]
	{
		HolidayType.Christmas,
		HolidayType.AprilFools
	};
}
