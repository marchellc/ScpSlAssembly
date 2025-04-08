using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NorthwoodLib.Pools;
using UnityEngine;

namespace UserSettings.VideoSettings
{
	public static class DisplayVideoSettings
	{
		public static event Action OnDisplayChanged;

		public static int CurrentDisplayIndex { get; private set; }

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
		}

		private static void SetupDisplayChangeDetector()
		{
			DisplayVideoSettings.CurrentDisplayIndex = -1;
			StaticUnityMethods.OnUpdate += delegate
			{
				int @int = PlayerPrefs.GetInt("UnitySelectMonitor");
				if (@int == DisplayVideoSettings.CurrentDisplayIndex || DisplayVideoSettings._isChangingDisplay)
				{
					return;
				}
				DisplayVideoSettings.CurrentDisplayIndex = @int;
				Action onDisplayChanged = DisplayVideoSettings.OnDisplayChanged;
				if (onDisplayChanged != null)
				{
					onDisplayChanged();
				}
				DisplayVideoSettings.UpdateSettings(false);
			};
		}

		private static void SetDefaultValues()
		{
			UserSetting<int>.SetDefaultValue<DisplayVideoSetting>(DisplayVideoSetting.VSyncCount, 1);
			UserSetting<float>.SetDefaultValue<DisplayVideoSetting>(DisplayVideoSetting.FpsLimiter, 60f);
		}

		private static void OnAspectRatioChanged(int newRatio)
		{
			Resolution[] selectedAspectResolutions = DisplayVideoSettings.GetSelectedAspectResolutions(newRatio);
			int num = selectedAspectResolutions.Length - 1;
			int num2 = int.MaxValue;
			int num3 = int.MaxValue;
			for (int i = 0; i < selectedAspectResolutions.Length; i++)
			{
				Resolution resolution = selectedAspectResolutions[i];
				int num4 = Mathf.Abs(resolution.height - Screen.height);
				if (num4 <= num2)
				{
					int num5 = Mathf.Abs(resolution.width - Screen.width);
					if (num4 != num2 || num5 <= num3)
					{
						num = i;
						num2 = num4;
						num3 = num5;
					}
				}
			}
			UserSetting<int>.Set<DisplayVideoSetting>(DisplayVideoSetting.Resolution, num);
		}

		private static void UpdateSettings(bool onLoad = false)
		{
			HashSet<uint> hashSet = HashSetPool<uint>.Shared.Rent();
			List<Resolution> list = ListPool<Resolution>.Shared.Rent();
			foreach (Resolution resolution in Screen.resolutions)
			{
				uint num = (uint)((int)((ushort)resolution.width) | (resolution.height << 16));
				if (hashSet.Add(num))
				{
					list.Add(resolution);
				}
			}
			DisplayVideoSettings._supportedResolutions = list.ToArray();
			ListPool<Resolution>.Shared.Return(list);
			DisplayVideoSettings.<UpdateSettings>g__RefreshFramerate|16_0();
			FullScreenMode fullScreenMode = (FullScreenMode)UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.FullscreenMode);
			Resolution[] selectedAspectResolutions = DisplayVideoSettings.GetSelectedAspectResolutions(UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.AspectRatio));
			int num2 = ((selectedAspectResolutions != null) ? selectedAspectResolutions.Length : 0);
			if (num2 == 0)
			{
				Screen.fullScreenMode = fullScreenMode;
				return;
			}
			int num3 = UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.Resolution, num2 - 1, true);
			Resolution resolution2 = selectedAspectResolutions[Mathf.Clamp(num3, 0, num2 - 1)];
			Screen.SetResolution(resolution2.width, resolution2.height, fullScreenMode);
			if (!onLoad || fullScreenMode != FullScreenMode.ExclusiveFullScreen || DisplayVideoSettings._alreadyRunning)
			{
				return;
			}
			DisplayVideoSettings._alreadyRunning = true;
			DisplayVideoSettings._desiredWidth = resolution2.width;
			DisplayVideoSettings._desiredHeight = resolution2.height;
			StaticUnityMethods.OnUpdate += DisplayVideoSettings.UpdateExclusiveFullScreen;
		}

		private static Resolution[] GetUnsupportedResolutions()
		{
			List<Resolution> list = ListPool<Resolution>.Shared.Rent();
			foreach (Resolution resolution in DisplayVideoSettings._supportedResolutions)
			{
				if (!DisplayVideoSettings.IsSupportedRatio(resolution))
				{
					list.Add(resolution);
				}
			}
			Resolution[] array = list.ToArray();
			ListPool<Resolution>.Shared.Return(list);
			return array;
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
			foreach (Resolution resolution in DisplayVideoSettings._supportedResolutions)
			{
				if (aspectRatio.CheckRes(resolution))
				{
					list.Add(resolution);
				}
			}
			Resolution[] array = list.ToArray();
			ListPool<Resolution>.Shared.Return(list);
			return array;
		}

		public static bool IsSupportedRatio(Resolution res)
		{
			foreach (AspectRatio aspectRatio in DisplayVideoSettings.SupportedRatios)
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
				DisplayInfo displayInfo = list[displayIndex];
				ListPool<DisplayInfo>.Shared.Return(list);
				AsyncOperation async = Screen.MoveMainWindowTo(in displayInfo, default(Vector2Int));
				while (!async.isDone)
				{
					await Task.Delay(Mathf.CeilToInt(Time.deltaTime * 1000f));
				}
				UserSetting<int>.Set<DisplayVideoSetting>(DisplayVideoSetting.AspectRatio, 0);
				DisplayVideoSettings.OnAspectRatioChanged(0);
				DisplayVideoSettings._isChangingDisplay = false;
			}
		}

		private static void UpdateExclusiveFullScreen()
		{
			if (!Input.anyKeyDown || Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
			{
				return;
			}
			Screen.SetResolution(DisplayVideoSettings._desiredWidth, DisplayVideoSettings._desiredHeight, FullScreenMode.ExclusiveFullScreen);
			StaticUnityMethods.OnUpdate -= DisplayVideoSettings.UpdateExclusiveFullScreen;
			DisplayVideoSettings._alreadyRunning = false;
		}

		[CompilerGenerated]
		internal static void <UpdateSettings>g__RefreshFramerate|16_0()
		{
			if (!DisplayVideoSettings._isPlaying)
			{
				QualitySettings.vSyncCount = 0;
				Application.targetFrameRate = 60;
				return;
			}
			QualitySettings.vSyncCount = UserSetting<int>.Get<DisplayVideoSetting>(DisplayVideoSetting.VSyncCount);
			Application.targetFrameRate = (UserSetting<bool>.Get<DisplayVideoSetting>(DisplayVideoSetting.FpsLimiter) ? Mathf.RoundToInt(UserSetting<float>.Get<DisplayVideoSetting>(DisplayVideoSetting.FpsLimiter)) : (-1));
		}

		private const string PrefsKey = "UnitySelectMonitor";

		private static Resolution[] _supportedResolutions;

		private static bool _isChangingDisplay;

		private static bool _isPlaying;

		public static readonly AspectRatio[] SupportedRatios = new AspectRatio[]
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
	}
}
