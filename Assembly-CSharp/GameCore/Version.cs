using MapGeneration.Holidays;
using Steam;

namespace GameCore;

public static class Version
{
	public enum VersionType : byte
	{
		Release,
		PublicRC,
		PublicBeta,
		PrivateRC,
		PrivateRCStreamingForbidden,
		PrivateBeta,
		PrivateBetaStreamingForbidden,
		Development,
		Nightly
	}

	public static readonly byte Major;

	public static readonly byte Minor;

	public static readonly byte Revision;

	public static readonly bool AlwaysAcceptReleaseBuilds;

	public static readonly VersionType BuildType;

	public static readonly HolidayType ActiveHoliday;

	public static readonly bool BackwardCompatibility;

	public static readonly byte BackwardRevision;

	public static readonly string DescriptionOverride;

	public static readonly string VersionString;

	public static bool PublicBeta
	{
		get
		{
			if (Version.BuildType != VersionType.PublicBeta)
			{
				return Version.BuildType == VersionType.PublicRC;
			}
			return true;
		}
	}

	public static bool PrivateBeta
	{
		get
		{
			VersionType buildType = Version.BuildType;
			return buildType == VersionType.PrivateBeta || buildType == VersionType.PrivateBetaStreamingForbidden || buildType == VersionType.PrivateRC || buildType == VersionType.PrivateRCStreamingForbidden || buildType == VersionType.Development || buildType == VersionType.Nightly;
		}
	}

	public static bool ReleaseCandidate
	{
		get
		{
			VersionType buildType = Version.BuildType;
			return buildType == VersionType.PublicRC || buildType == VersionType.PrivateRC || buildType == VersionType.PrivateRCStreamingForbidden;
		}
	}

	public static bool StreamingAllowed
	{
		get
		{
			VersionType buildType = Version.BuildType;
			if (buildType != VersionType.PrivateBetaStreamingForbidden && buildType != VersionType.PrivateRCStreamingForbidden && buildType != VersionType.Development)
			{
				return buildType != VersionType.Nightly;
			}
			return false;
		}
	}

	public static bool ExtendedVersionCheckNeeded => Version.BuildType != VersionType.Release;

	static Version()
	{
		Version.Major = 14;
		Version.Minor = 1;
		Version.Revision = 1;
		Version.AlwaysAcceptReleaseBuilds = false;
		Version.BuildType = VersionType.Release;
		Version.ActiveHoliday = HolidayType.None;
		Version.BackwardCompatibility = false;
		Version.BackwardRevision = 0;
		Version.DescriptionOverride = null;
		Version.VersionString = string.Format("{0}.{1}.{2}{3}", Version.Major, Version.Minor, Version.Revision, (!Version.ExtendedVersionCheckNeeded) ? string.Empty : ("-" + (Version.DescriptionOverride ?? "deploy-d69d84dc")));
		SteamServerInfo.Version = Version.VersionString;
	}

	public static bool CompatibilityCheck(byte sMajor, byte sMinor, byte sRevision, byte cMajor, byte cMinor, byte cRevision, bool cBackwardEnabled, byte cBackwardRevision)
	{
		if (sMajor != cMajor || sMinor != cMinor)
		{
			return false;
		}
		if (!cBackwardEnabled)
		{
			return sRevision == cRevision;
		}
		if (sRevision >= cBackwardRevision)
		{
			return sRevision <= cRevision;
		}
		return false;
	}
}
