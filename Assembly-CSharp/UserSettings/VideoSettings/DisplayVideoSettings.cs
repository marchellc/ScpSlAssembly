using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NorthwoodLib.Pools;
using UnityEngine;

namespace UserSettings.VideoSettings;

public static class DisplayVideoSettings
{
	private const string PrefsKey = "UnitySelectMonitor";

	private static Resolution[] _supportedResolutions;

	private static bool _isChangingDisplay;

	private static bool _isPlaying;

	public static readonly AspectRatio[] SupportedRatios = new AspectRatio[4]
	{
		new AspectRatio
		{
			Horizontal = 4f,
			Vertical = 3f,
			RatioMinMax = new Vector2(1.28f, 1.38f)
		},
		new AspectRatio
		{
			Horizontal = 16f,
			Vertical = 10f,
			RatioMinMax = new Vector2(1.59f, 1.61f)
		},
		new AspectRatio
		{
			Horizontal = 16f,
			Vertical = 9f,
			RatioMinMax = new Vector2(1.75f, 1.78f)
		},
		new AspectRatio
		{
			Horizontal = 21f,
			Vertical = 9f,
			RatioMinMax = new Vector2(2.27f, 2.39f)
		}
	};

	private static int _desiredWidth;

	private static int _desiredHeight;

	private static bool _alreadyRunning;

	public static int CurrentDisplayIndex { get; private set; }

	public static event Action OnDisplayChanged;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
	}

	private static void SetupDisplayChangeDetector()
	{
		DisplayVideoSettings.CurrentDisplayIndex = -1;
		StaticUnityMethods.OnUpdate += delegate
		{
			int num = PlayerPrefs.GetInt("UnitySelectMonitor");
			if (num != DisplayVideoSettings.CurrentDisplayIndex && !DisplayVideoSettings._isChangingDisplay)
			{
				DisplayVideoSettings.CurrentDisplayIndex = num;
				DisplayVideoSettings.OnDisplayChanged?.Invoke();
				DisplayVideoSettings.UpdateSettings();
			}
		};
	}

	private static void SetDefaultValues()
	{
		UserSetting<int>.SetDefaultValue(DisplayVideoSetting.VSyncCount, 1);
		UserSetting<float>.SetDefaultValue(DisplayVideoSetting.FpsLimiter, 60f);
	}

	private static void OnAspectRatioChanged(int newRatio)
	{
		Resolution[] selectedAspectResolutions = DisplayVideoSettings.GetSelectedAspectResolutions(newRatio);
		int value = selectedAspectResolutions.Length - 1;
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		for (int i = 0; i < selectedAspectResolutions.Length; i++)
		{
			Resolution resolution = selectedAspectResolutions[i];
			int num3 = Mathf.Abs(resolution.height - Screen.height);
			if (num3 <= num)
			{
				int num4 = Mathf.Abs(resolution.width - Screen.width);
				if (num3 != num || num4 <= num2)
				{
					value = i;
					num = num3;
					num2 = num4;
				}
			}
		}
		UserSetting<int>.Set(DisplayVideoSetting.Resolution, value);
	}

	private static void UpdateSettings(bool onLoad = false)
	{
		HashSet<uint> hashSet = HashSetPool<uint>.Shared.Rent();
		List<Resolution> list = ListPool<Resolution>.Shared.Rent();
		Resolution[] resolutions = Screen.resolutions;
		for (int i = 0; i < resolutions.Length; i++)
		{
			Resolution item = resolutions[i];
			uint item2 = (uint)((ushort)item.width | (item.height << 16));
			if (hashSet.Add(item2))
			{
				list.Add(item);
			}
		}
		DisplayVideoSettings._supportedResolutions = list.ToArray();
		ListPool<Resolution>.Shared.Return(list);
		RefreshFramerate();
		FullScreenMode fullScreenMode = (FullScreenMode)UserSetting<int>.Get(DisplayVideoSetting.FullscreenMode);
		Resolution[] selectedAspectResolutions = DisplayVideoSettings.GetSelectedAspectResolutions(UserSetting<int>.Get(DisplayVideoSetting.AspectRatio));
		int num = ((selectedAspectResolutions != null) ? selectedAspectResolutions.Length : 0);
		if (num == 0)
		{
			Screen.fullScreenMode = fullScreenMode;
			return;
		}
		int value = UserSetting<int>.Get(DisplayVideoSetting.Resolution, num - 1, setAsDefault: true);
		Resolution resolution = selectedAspectResolutions[Mathf.Clamp(value, 0, num - 1)];
		Screen.SetResolution(resolution.width, resolution.height, fullScreenMode);
		if (onLoad && fullScreenMode == FullScreenMode.ExclusiveFullScreen && !DisplayVideoSettings._alreadyRunning)
		{
			DisplayVideoSettings._alreadyRunning = true;
			DisplayVideoSettings._desiredWidth = resolution.width;
			DisplayVideoSettings._desiredHeight = resolution.height;
			StaticUnityMethods.OnUpdate += UpdateExclusiveFullScreen;
		}
		static void RefreshFramerate()
		{
			if (!DisplayVideoSettings._isPlaying)
			{
				QualitySettings.vSyncCount = 0;
				Application.targetFrameRate = 60;
			}
			else
			{
				QualitySettings.vSyncCount = UserSetting<int>.Get(DisplayVideoSetting.VSyncCount);
				Application.targetFrameRate = (UserSetting<bool>.Get(DisplayVideoSetting.FpsLimiter) ? Mathf.RoundToInt(UserSetting<float>.Get(DisplayVideoSetting.FpsLimiter)) : (-1));
			}
		}
	}

	private static Resolution[] GetUnsupportedResolutions()
	{
		List<Resolution> list = ListPool<Resolution>.Shared.Rent();
		Resolution[] supportedResolutions = DisplayVideoSettings._supportedResolutions;
		foreach (Resolution resolution in supportedResolutions)
		{
			if (!DisplayVideoSettings.IsSupportedRatio(resolution))
			{
				list.Add(resolution);
			}
		}
		Resolution[] result = list.ToArray();
		ListPool<Resolution>.Shared.Return(list);
		return result;
	}

	public static Resolution[] GetSelectedAspectResolutions(int filterId)
	{
		if (filterId <= 0)
		{
			return DisplayVideoSettings._supportedResolutions;
		}
		if (filterId > DisplayVideoSettings.SupportedRatios.Length)
		{
			return DisplayVideoSettings.GetUnsupportedResolutions();
		}
		AspectRatio aspectRatio = DisplayVideoSettings.SupportedRatios[filterId - 1];
		List<Resolution> list = ListPool<Resolution>.Shared.Rent();
		Resolution[] supportedResolutions = DisplayVideoSettings._supportedResolutions;
		foreach (Resolution resolution in supportedResolutions)
		{
			if (aspectRatio.CheckRes(resolution))
			{
				list.Add(resolution);
			}
		}
		Resolution[] result = list.ToArray();
		ListPool<Resolution>.Shared.Return(list);
		return result;
	}

	public static bool IsSupportedRatio(Resolution res)
	{
		AspectRatio[] supportedRatios = DisplayVideoSettings.SupportedRatios;
		foreach (AspectRatio aspectRatio in supportedRatios)
		{
			if (aspectRatio.CheckRes(res))
			{
				return true;
			}
		}
		return false;
	}

	public static async void ChangeDisplay(int displayIndex)
	{
		if (!DisplayVideoSettings._isChangingDisplay)
		{
			DisplayVideoSettings._isChangingDisplay = true;
			List<DisplayInfo> list = ListPool<DisplayInfo>.Shared.Rent();
			Screen.GetDisplayLayout(list);
			DisplayInfo display = list[displayIndex];
			ListPool<DisplayInfo>.Shared.Return(list);
			AsyncOperation async = Screen.MoveMainWindowTo(in display, default(Vector2Int));
			while (!async.isDone)
			{
				await Task.Delay(Mathf.CeilToInt(Time.deltaTime * 1000f));
			}
			UserSetting<int>.Set(DisplayVideoSetting.AspectRatio, 0);
			DisplayVideoSettings.OnAspectRatioChanged(0);
			DisplayVideoSettings._isChangingDisplay = false;
		}
	}

	private static void UpdateExclusiveFullScreen()
	{
		if (Input.anyKeyDown && Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
		{
			Screen.SetResolution(DisplayVideoSettings._desiredWidth, DisplayVideoSettings._desiredHeight, FullScreenMode.ExclusiveFullScreen);
			StaticUnityMethods.OnUpdate -= UpdateExclusiveFullScreen;
			DisplayVideoSettings._alreadyRunning = false;
		}
	}
}
