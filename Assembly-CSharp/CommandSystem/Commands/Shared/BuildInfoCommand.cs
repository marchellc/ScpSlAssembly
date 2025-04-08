using System;
using GameCore;
using UnityEngine;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class BuildInfoCommand : ICommand
	{
		public string Command { get; } = "buildinfo";

		public string[] Aliases { get; } = new string[] { "v", "ver", "version" };

		public string Description { get; } = "Displays information about the current build.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = BuildInfoCommand.BuildInfoString;
			return true;
		}

		internal static string BuildInfoString
		{
			get
			{
				return string.Concat(new string[]
				{
					"Build info:\nGame version: ",
					global::GameCore.Version.VersionString,
					string.Format("\nPreauth version: {0}.{1}.{2}", global::GameCore.Version.Major, global::GameCore.Version.Minor, global::GameCore.Version.Revision),
					"\nBackward compatibility: ",
					(!global::GameCore.Version.BackwardCompatibility || global::GameCore.Version.ExtendedVersionCheckNeeded) ? "False" : string.Format("{0}.{1}.{2} and newer", global::GameCore.Version.Major, global::GameCore.Version.Minor, global::GameCore.Version.BackwardRevision),
					"\nBuild timestamp: 2025-04-04 16:40:29Z",
					string.Format("\nBuild type: {0}", global::GameCore.Version.BuildType),
					string.Format("\nHoliday type: {0}", global::GameCore.Version.ActiveHoliday),
					string.Format("\nAlways accept release builds: {0}", global::GameCore.Version.AlwaysAcceptReleaseBuilds),
					"\nBuild GUID: ",
					PlatformInfo.singleton.BuildGuid,
					"\nUnity version: ",
					Application.unityVersion,
					string.Format("\n\nPrivate beta: {0}", global::GameCore.Version.PrivateBeta),
					string.Format("\nPublic beta: {0}", global::GameCore.Version.PublicBeta),
					string.Format("\nRelease candidate: {0}", global::GameCore.Version.ReleaseCandidate),
					string.Format("\nStreaming allowed: {0}", global::GameCore.Version.StreamingAllowed),
					string.Format("\nHeadless: {0}", PlatformInfo.singleton.IsHeadless),
					"\nModded: ",
					CustomNetworkManager.Modded ? string.Format("{0}\nMod Description:\n{1}", true, BuildInfoCommand.ModDescription) : false.ToString()
				});
			}
		}

		public static string ModDescription;
	}
}
