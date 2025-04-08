using System;

public static class TimeBehaviour
{
	public static long CurrentTimestamp()
	{
		return DateTime.UtcNow.Ticks;
	}

	public static long CurrentUnixTimestamp
	{
		get
		{
			return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}
	}

	public static long GetBanExpirationTime(uint seconds)
	{
		return DateTime.UtcNow.AddSeconds(seconds).Ticks;
	}

	public static bool ValidateTimestamp(long timestampentry, long timestampexit, long limit)
	{
		return timestampexit - timestampentry < limit;
	}

	public static string Rfc3339Time()
	{
		return TimeBehaviour.Rfc3339Time(DateTimeOffset.Now);
	}

	public static string Rfc3339Time(DateTimeOffset date)
	{
		return TimeBehaviour.FormatTime((date.Offset == TimeSpan.Zero) ? "yyyy-MM-dd HH:mm:ss.fffZ" : "yyyy-MM-dd HH:mm:ss.fff zzz", date);
	}

	public static string FormatTime(string format)
	{
		return TimeBehaviour.FormatTime(format, DateTimeOffset.Now);
	}

	public static string FormatTime(string format, DateTimeOffset date)
	{
		return format.Replace("yyyy", date.Year.ToString()).Replace("MM", Misc.LeadingZeroes(date.Month, 2U, false)).Replace("M", date.Month.ToString())
			.Replace("dd", Misc.LeadingZeroes(date.Day, 2U, false))
			.Replace("d", date.Day.ToString())
			.Replace("HH", Misc.LeadingZeroes(date.Hour, 2U, false))
			.Replace("H", date.Hour.ToString())
			.Replace("mm", Misc.LeadingZeroes(date.Minute, 2U, false))
			.Replace("m", date.Minute.ToString())
			.Replace("ss", Misc.LeadingZeroes(date.Second, 2U, false))
			.Replace("s", date.Second.ToString())
			.Replace("fff", Misc.LeadingZeroes(date.Millisecond, 3U, false))
			.Replace("ff", Misc.LeadingZeroes(date.Millisecond / 10, 2U, false))
			.Replace("f", (date.Millisecond / 100).ToString())
			.Replace("zzz", Misc.LeadingZeroes(date.Offset.Hours, 2U, true) + ":" + Misc.LeadingZeroes(date.Offset.Minutes, 2U, false))
			.Replace("zz", Misc.LeadingZeroes(date.Offset.Hours, 2U, true))
			.Replace("z", Misc.LeadingZeroes(date.Offset.Hours, 1U, true));
	}
}
