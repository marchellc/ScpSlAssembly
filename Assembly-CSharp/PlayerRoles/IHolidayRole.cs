using System;
using GameCore;
using MapGeneration.Holidays;

namespace PlayerRoles
{
	public interface IHolidayRole
	{
		bool IsAvailable
		{
			get
			{
				if (this.TargetHolidays.IsAnyHolidayActive(true, true))
				{
					return true;
				}
				global::GameCore.Version.VersionType buildType = global::GameCore.Version.BuildType;
				return buildType == global::GameCore.Version.VersionType.Development || buildType == global::GameCore.Version.VersionType.Nightly;
			}
		}

		HolidayType[] TargetHolidays { get; }
	}
}
