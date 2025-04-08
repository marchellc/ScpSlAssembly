using System;
using MapGeneration.Holidays;
using Steam;

namespace GameCore
{
	public static class Version
	{
		static Version()
		{
			SteamServerInfo.Version = Version.VersionString;
		}

		public static bool PublicBeta
		{
			get
			{
				return Version.BuildType == Version.VersionType.PublicBeta || Version.BuildType == Version.VersionType.PublicRC;
			}
		}

		public static bool PrivateBeta
		{
			get
			{
				Version.VersionType buildType = Version.BuildType;
				return buildType == Version.VersionType.PrivateBeta || buildType == Version.VersionType.PrivateBetaStreamingForbidden || buildType == Version.VersionType.PrivateRC || buildType == Version.VersionType.PrivateRCStreamingForbidden || buildType == Version.VersionType.Development || buildType == Version.VersionType.Nightly;
			}
		}

		public static bool ReleaseCandidate
		{
			get
			{
				Version.VersionType buildType = Version.BuildType;
				return buildType == Version.VersionType.PublicRC || buildType == Version.VersionType.PrivateRC || buildType == Version.VersionType.PrivateRCStreamingForbidden;
			}
		}

		public static bool StreamingAllowed
		{
			get
			{
				Version.VersionType buildType = Version.BuildType;
				return buildType != Version.VersionType.PrivateBetaStreamingForbidden && buildType != Version.VersionType.PrivateRCStreamingForbidden && buildType != Version.VersionType.Development && buildType != Version.VersionType.Nightly;
			}
		}

		public static bool ExtendedVersionCheckNeeded
		{
			get
			{
				return Version.BuildType > Version.VersionType.Release;
			}
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
			return sRevision >= cBackwardRevision && sRevision <= cRevision;
		}

		public static readonly byte Major = 14;

		public static readonly byte Minor = 0;

		public static readonly byte Revision = 3;

		public static readonly bool AlwaysAcceptReleaseBuilds = false;

		public static readonly Version.VersionType BuildType = Version.VersionType.PublicBeta;

		public static readonly HolidayType ActiveHoliday = HolidayType.None;

		public static readonly bool BackwardCompatibility = false;

		public static readonly byte BackwardRevision = 0;

		public static readonly string DescriptionOverride = null;

		public static readonly string VersionString = string.Format("{0}.{1}.{2}{3}", new object[]
		{
			Version.Major,
			Version.Minor,
			Version.Revision,
			(!Version.ExtendedVersionCheckNeeded) ? string.Empty : ("-" + (Version.DescriptionOverride ?? "labapi-publicbeta-0fab0c82"))
		});

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
	}
}
