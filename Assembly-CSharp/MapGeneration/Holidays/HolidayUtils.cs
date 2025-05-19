using GameCore;

namespace MapGeneration.Holidays;

public static class HolidayUtils
{
	public static bool HolidaysEnabled { get; set; } = true;

	public static HolidayType GetActiveHoliday(bool mustBeForced = false, bool ignoreServerConfig = false)
	{
		if (!ignoreServerConfig && !HolidaysEnabled)
		{
			return HolidayType.None;
		}
		if (mustBeForced || Version.ActiveHoliday != 0)
		{
			return Version.ActiveHoliday;
		}
		return HolidayType.None;
	}

	public static bool IsHolidayActive(HolidayType holiday, bool mustBeForced = false, bool ignoreServerConfig = false)
	{
		return GetActiveHoliday(mustBeForced, ignoreServerConfig) == holiday;
	}

	public static bool IsAnyHolidayActive(this HolidayType[] holidays, bool mustBeForced = false, bool ignoreServerConfig = false)
	{
		if (holidays == null || holidays.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < holidays.Length; i++)
		{
			if (IsHolidayActive(holidays[i], mustBeForced, ignoreServerConfig))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsAnyHolidayActive(bool mustBeForced = false, bool ignoreServerConfig = false)
	{
		return GetActiveHoliday(mustBeForced, ignoreServerConfig) != HolidayType.None;
	}

	public static bool TryGetResult<TArray, TValue>(this TArray[] holidayData, out TValue result) where TArray : IHolidayFetchableData<TValue>
	{
		if (holidayData == null)
		{
			result = default(TValue);
			return false;
		}
		foreach (IHolidayFetchableData<TValue> holidayFetchableData in holidayData)
		{
			if (IsHolidayActive(holidayFetchableData.Holiday))
			{
				result = holidayFetchableData.Result;
				return true;
			}
		}
		result = default(TValue);
		return false;
	}
}
