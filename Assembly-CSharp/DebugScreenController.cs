using System;
using System.Globalization;
using System.IO;
using System.Threading;
using CommandSystem.Commands.Shared;
using GameCore;
using NorthwoodLib;
using UnityEngine;
using UserSettings;
using UserSettings.UserInterfaceSettings;

public class DebugScreenController : MonoBehaviour
{
	private void Awake()
	{
		Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.GetFullPath(Application.dataPath)));
		if (!Environment.GetCommandLineArgs().Contains("-nographics"))
		{
			Shutdown.Quit(true, false);
		}
	}

	private void Start()
	{
		global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Application.logMessageReceivedThreaded += DebugScreenController.LogMessage;
		DebugScreenController.Log();
	}

	private static void Log()
	{
		if (DebugScreenController._logged)
		{
			return;
		}
		Debug.Log(string.Concat(new string[]
		{
			"Time: ",
			TimeBehaviour.Rfc3339Time(),
			"\nGPU: ",
			SystemInfo.graphicsDeviceName,
			"\nGPU Driver version: ",
			GpuDriver.DriverVersion,
			"\nVRAM: ",
			SystemInfo.graphicsMemorySize.ToString(),
			"MB\nShaderLevel: ",
			SystemInfo.graphicsShaderLevel.ToString().Insert(1, "."),
			"\nVendor: ",
			SystemInfo.graphicsDeviceVendor,
			"\nAPI: ",
			SystemInfo.graphicsDeviceType.ToString(),
			"\nInfo: ",
			SystemInfo.graphicsDeviceVersion,
			"\nResolution: ",
			Screen.width.ToString(),
			"x",
			Screen.height.ToString(),
			"\nFPS Limit: ",
			Application.targetFrameRate.ToString(),
			"\nFullscreen: ",
			Screen.fullScreenMode.ToString(),
			"\nCPU: ",
			SystemInfo.processorType,
			"\nThreads: ",
			SystemInfo.processorCount.ToString(),
			"\nFrequency: ",
			SystemInfo.processorFrequency.ToString(),
			"MHz\nRAM: ",
			SystemInfo.systemMemorySize.ToString(),
			"MB\nAudio Supported: ",
			SystemInfo.supportsAudio.ToString(),
			"\nOS: ",
			global::NorthwoodLib.OperatingSystem.VersionString,
			"\nUnity: ",
			Application.unityVersion,
			"\nFramework: ",
			Misc.GetRuntimeVersion(),
			"\nIL2CPP: ",
			PlatformInfo.singleton.IsIl2Cpp.ToString(),
			"\nVersion: ",
			global::GameCore.Version.VersionString,
			"\nBuild: ",
			Application.buildGUID,
			"\nSystem Language: ",
			CultureInfo.CurrentCulture.EnglishName,
			" (",
			CultureInfo.CurrentCulture.Name,
			")\nGame Language: ",
			UserSetting<string>.Get<UISetting>(UISetting.Language, "en", false),
			"\nLaunch arguments: ",
			Environment.CommandLine
		}));
		Debug.Log(BuildInfoCommand.BuildInfoString);
		if (WindowsUpdateWarning.UpdateRequired())
		{
			Debug.LogError("Important system file that is needed for voicechat is missing, please install this windows update in order to get your voicechat working https://support.microsoft.com/en-us/help/2999226/update-for-universal-c-runtime-in-windows");
		}
	}

	private static void LogMessage(string condition, string stackTrace, LogType type)
	{
		switch (type)
		{
		case LogType.Error:
			Interlocked.Increment(ref DebugScreenController.Errors);
			return;
		case LogType.Assert:
			Interlocked.Increment(ref DebugScreenController.Asserts);
			return;
		case LogType.Warning:
		case LogType.Log:
			break;
		case LogType.Exception:
			Interlocked.Increment(ref DebugScreenController.Exceptions);
			break;
		default:
			return;
		}
	}

	public static int Asserts;

	public static int Errors;

	public static int Exceptions;

	private static bool _logged;
}
