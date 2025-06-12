using System;
using UnityEngine;

namespace UserSettings.AudioSettings;

public class SettingBasedRolloffCurve : MonoBehaviour
{
	[Serializable]
	private struct PresetCurvePair
	{
		public int Value;

		public AnimationCurve FalloffCurve;
	}

	[SerializeField]
	private OtherAudioSetting _trackedSetting;

	[SerializeField]
	private PresetCurvePair[] _pairs;

	private AnimationCurve _defaultCurve;

	private AudioSource _srcCache;

	private AnimationCurve CurrentCurve
	{
		get
		{
			return this.Source.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
		}
		set
		{
			this.Source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, value);
		}
	}

	private AudioSource Source
	{
		get
		{
			if ((object)this._srcCache == null)
			{
				this._srcCache = base.GetComponent<AudioSource>();
			}
			return this._srcCache;
		}
	}

	private void Awake()
	{
		this._defaultCurve = this.Source.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
		this.UpdateCurve(UserSetting<int>.Get(this._trackedSetting));
		UserSetting<int>.AddListener(this._trackedSetting, UpdateCurve);
	}

	private void OnDestroy()
	{
		UserSetting<int>.RemoveListener(this._trackedSetting, UpdateCurve);
	}

	private void Reset()
	{
		if (this._pairs == null || this._pairs.Length == 0)
		{
			PresetCurvePair presetCurvePair = new PresetCurvePair
			{
				FalloffCurve = this.CurrentCurve
			};
			this._pairs = new PresetCurvePair[1] { presetCurvePair };
		}
	}

	private void UpdateCurve(int settingValue)
	{
		PresetCurvePair[] pairs = this._pairs;
		for (int i = 0; i < pairs.Length; i++)
		{
			PresetCurvePair presetCurvePair = pairs[i];
			if (presetCurvePair.Value == settingValue)
			{
				this.CurrentCurve = presetCurvePair.FalloffCurve;
				return;
			}
		}
		this.CurrentCurve = this._defaultCurve;
	}
}
