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
	public static int Asserts;

	public static int Errors;

	public static int Exceptions;

	private static bool _logged;

	private void Awake()
	{
		Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.GetFullPath(Application.dataPath)));
		if (!Environment.GetCommandLineArgs().Contains<string>("-nographics"))
		{
			Shutdown.Quit();
		}
	}

	private void Start()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Application.logMessageReceivedThreaded += LogMessage;
		Log();
	}

	private static void Log()
	{
		if (!_logged)
		{
			Debug.Log("Time: " + TimeBehaviour.Rfc3339Time() + "\nGPU: " + SystemInfo.graphicsDeviceName + "\nGPU Driver version: " + GpuDriver.DriverVersion + "\nVRAM: " + SystemInfo.graphicsMemorySize + "MB\nShaderLevel: " + SystemInfo.graphicsShaderLevel.ToString().Insert(1, ".") + "\nVendor: " + SystemInfo.graphicsDeviceVendor + "\nAPI: " + SystemInfo.graphicsDeviceType.ToString() + "\nInfo: " + SystemInfo.graphicsDeviceVersion + "\nResolution: " + Screen.width + "x" + Screen.height + "\nFPS Limit: " + Application.targetFrameRate + "\nFullscreen: " + Screen.fullScreenMode.ToString() + "\nCPU: " + SystemInfo.processorType + "\nThreads: " + SystemInfo.processorCount + "\nFrequency: " + SystemInfo.processorFrequency + "MHz\nRAM: " + SystemInfo.systemMemorySize + "MB\nAudio Supported: " + SystemInfo.supportsAudio + "\nOS: " + NorthwoodLib.OperatingSystem.VersionString + "\nUnity: " + Application.unityVersion + "\nFramework: " + Misc.GetRuntimeVersion() + "\nIL2CPP: " + PlatformInfo.singleton.IsIl2Cpp + "\nVersion: " + GameCore.Version.VersionString + "\nBuild: " + Application.buildGUID + "\nSystem Language: " + CultureInfo.CurrentCulture.EnglishName + " (" + CultureInfo.CurrentCulture.Name + ")\nGame Language: " + UserSetting<string>.Get(UISetting.Language, "en") + "\nLaunch arguments: " + Environment.CommandLine);
			Debug.Log(BuildInfoCommand.BuildInfoString);
			if (WindowsUpdateWarning.UpdateRequired())
			{
				Debug.LogError("Important system file that is needed for voicechat is missing, please install this windows update in order to get your voicechat working https://support.microsoft.com/en-us/help/2999226/update-for-universal-c-runtime-in-windows");
			}
		}
	}

	private static void LogMessage(string condition, string stackTrace, LogType type)
	{
		switch (type)
		{
		case LogType.Assert:
			Interlocked.Increment(ref Asserts);
			break;
		case LogType.Error:
			Interlocked.Increment(ref Errors);
			break;
		case LogType.Exception:
			Interlocked.Increment(ref Exceptions);
			break;
		case LogType.Warning:
		case LogType.Log:
			break;
		}
	}
}
