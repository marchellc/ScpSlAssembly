using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.Filmmaker
{
	public static class FilmmakerTimelineManager
	{
		public static float TimeSeconds { get; set; }

		public static int TimeFrames
		{
			get
			{
				return Mathf.RoundToInt(FilmmakerTimelineManager.TimeSeconds * 50f);
			}
			set
			{
				FilmmakerTimelineManager.TimeSeconds = (float)value * 0.02f;
			}
		}

		public static void UpdatePlayback()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			FilmmakerRole filmmakerRole = referenceHub.roleManager.CurrentRole as FilmmakerRole;
			if (filmmakerRole == null)
			{
				return;
			}
			filmmakerRole.CameraPosition = FilmmakerTimelineManager.EvaluateParam<Vector3>(FilmmakerTimelineManager.PositionTrack, new Func<Vector3, Vector3, float, Vector3>(Vector3.Lerp));
			filmmakerRole.CameraRotation = FilmmakerTimelineManager.EvaluateParam<Quaternion>(FilmmakerTimelineManager.RotationTrack, new Func<Quaternion, Quaternion, float, Quaternion>(Quaternion.Lerp));
			FilmmakerRole.ZoomScale = FilmmakerTimelineManager.EvaluateParam<float>(FilmmakerTimelineManager.ZoomTrack, new Func<float, float, float, float>(Mathf.Lerp));
		}

		private static T EvaluateParam<T>(FilmmakerTrack<T> selector, Func<T, T, float, T> lerp) where T : struct
		{
			FilmmakerKeyframe<T>[] keyframes = selector.Keyframes;
			FilmmakerKeyframe<T> filmmakerKeyframe = null;
			FilmmakerKeyframe<T> filmmakerKeyframe2 = null;
			int timeFrames = FilmmakerTimelineManager.TimeFrames;
			for (int i = keyframes.Length - 1; i >= 0; i--)
			{
				if (keyframes[i].TimeFrames <= timeFrames)
				{
					filmmakerKeyframe = keyframes[i];
					break;
				}
			}
			for (int j = 0; j < keyframes.Length; j++)
			{
				if (keyframes[j].TimeFrames >= timeFrames)
				{
					filmmakerKeyframe2 = keyframes[j];
					break;
				}
			}
			if (filmmakerKeyframe == null)
			{
				filmmakerKeyframe = filmmakerKeyframe2;
			}
			if (filmmakerKeyframe2 == null)
			{
				filmmakerKeyframe2 = filmmakerKeyframe;
			}
			if (filmmakerKeyframe2 == null || filmmakerKeyframe == null)
			{
				return selector.DefaultValue;
			}
			AnimationCurve animationCurve = FilmmakerTimelineManager.BlendPresets[filmmakerKeyframe.BlendCurve];
			float num = (float)filmmakerKeyframe.TimeFrames * 0.02f;
			float num2 = (float)filmmakerKeyframe2.TimeFrames * 0.02f;
			float num3 = Mathf.InverseLerp(num, num2, FilmmakerTimelineManager.TimeSeconds);
			return lerp(filmmakerKeyframe.Value, filmmakerKeyframe2.Value, animationCurve.Evaluate(num3));
		}

		// Note: this type is marked as 'beforefieldinit'.
		static FilmmakerTimelineManager()
		{
			Dictionary<FilmmakerBlendPreset, AnimationCurve> dictionary = new Dictionary<FilmmakerBlendPreset, AnimationCurve>();
			dictionary[FilmmakerBlendPreset.Linear] = AnimationCurve.Linear(0f, 0f, 1f, 1f);
			dictionary[FilmmakerBlendPreset.Smooth] = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
			dictionary[FilmmakerBlendPreset.Hold] = AnimationCurve.Constant(0f, 1f, 0f);
			dictionary[FilmmakerBlendPreset.FetchNext] = AnimationCurve.Constant(0f, 1f, 1f);
			FilmmakerTimelineManager.BlendPresets = dictionary;
		}

		public const float FrameRate = 50f;

		public const float InvFrameRate = 0.02f;

		public static readonly FilmmakerTrack<Vector3> PositionTrack = new FilmmakerTrack<Vector3>(Vector3.up * 1001f);

		public static readonly FilmmakerTrack<Quaternion> RotationTrack = new FilmmakerTrack<Quaternion>(Quaternion.identity);

		public static readonly FilmmakerTrack<float> ZoomTrack = new FilmmakerTrack<float>(1f);

		public static readonly IFilmmakerTrack[] AllTracks = new IFilmmakerTrack[]
		{
			FilmmakerTimelineManager.PositionTrack,
			FilmmakerTimelineManager.RotationTrack,
			FilmmakerTimelineManager.ZoomTrack
		};

		private static readonly Dictionary<FilmmakerBlendPreset, AnimationCurve> BlendPresets;
	}
}
