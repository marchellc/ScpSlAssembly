using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class Headless
{
	public static readonly string version = "1.6.4";

	private static bool isHeadless = false;

	private static bool checkedHeadless = false;

	private static bool initializedHeadless = false;

	private static bool buildingHeadless = false;

	private static bool debuggingHeadless = false;

	private static HeadlessRuntime headlessRuntime;

	private static string currentProfile = "";

	public static string GetProfileName()
	{
		if (!Headless.IsHeadless())
		{
			return null;
		}
		Headless.InitializeHeadless();
		return Headless.currentProfile;
	}

	public static bool IsHeadless()
	{
		if (Headless.checkedHeadless)
		{
			return Headless.isHeadless;
		}
		if (File.Exists(Application.dataPath + "/~HeadlessDebug.txt"))
		{
			Headless.debuggingHeadless = true;
			Headless.isHeadless = true;
		}
		else if (Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0)
		{
			Headless.isHeadless = true;
		}
		else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			Headless.isHeadless = true;
		}
		Headless.checkedHeadless = true;
		return Headless.isHeadless;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void OnBeforeSceneLoadRuntimeMethod()
	{
		if (Headless.IsHeadless())
		{
			Headless.InitializeHeadless();
			HeadlessCallbacks.InvokeCallbacks("HeadlessBeforeSceneLoad");
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void OnAfterSceneLoadRuntimeMethod()
	{
		if (!Headless.IsHeadless())
		{
			return;
		}
		if (Headless.headlessRuntime.valueCamera)
		{
			GameObject gameObject = GameObject.Find("HeadlessBehaviour");
			if (gameObject == null)
			{
				gameObject = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("HeadlessBehaviour"));
			}
			HeadlessBehaviour headlessBehaviour = gameObject.GetComponent<HeadlessBehaviour>();
			if (headlessBehaviour == null)
			{
				headlessBehaviour = gameObject.AddComponent<HeadlessBehaviour>();
			}
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(headlessBehaviour.GetComponent<HeadlessBehaviour>().NullifyCamera));
		}
		HeadlessCallbacks.InvokeCallbacks("HeadlessAfterSceneLoad");
	}

	private static void InitializeHeadless()
	{
		if (Headless.initializedHeadless)
		{
			return;
		}
		Headless.headlessRuntime = Resources.Load("HeadlessRuntime") as HeadlessRuntime;
		if (Headless.headlessRuntime != null)
		{
			Headless.currentProfile = Headless.headlessRuntime.profileName;
			if (Headless.headlessRuntime.valueLimitFramerate)
			{
				Application.targetFrameRate = Headless.headlessRuntime.valueFramerate;
				QualitySettings.vSyncCount = 0;
				Debug.Log("Application target framerate set to " + Headless.headlessRuntime.valueFramerate);
			}
		}
		Headless.initializedHeadless = true;
		HeadlessCallbacks.InvokeCallbacks("HeadlessBeforeFirstSceneLoad");
	}

	private static void HandleLog(string logString, string stackTrace, LogType type)
	{
		Console.WriteLine(logString);
		if (stackTrace.Length > 1)
		{
			Console.WriteLine("in: " + stackTrace);
		}
	}

	public static bool IsBuildingHeadless()
	{
		if (Headless.buildingHeadless)
		{
			return true;
		}
		return false;
	}

	public static bool IsDebuggingHeadless()
	{
		if (Headless.debuggingHeadless)
		{
			return true;
		}
		return false;
	}

	public static void SetBuildingHeadless(bool value, string profileName)
	{
		Headless.buildingHeadless = value;
		Headless.currentProfile = profileName;
	}
}
