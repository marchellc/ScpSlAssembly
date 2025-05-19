using UnityEngine;
using UnityEngine.Audio;

namespace UserSettings.AudioSettings;

public class MixerAudioSettings : MonoBehaviour
{
	private class AudioSlider
	{
		private readonly VolumeSliderSetting _setting;

		private readonly string _floatName;

		private AudioMixer _mixer;

		private bool _mixerSet;

		public AudioSlider(VolumeSliderSetting setting, string floatName)
		{
			_setting = setting;
			_floatName = floatName;
			UserSetting<float>.AddListener(_setting, SetFloat);
			UserSetting<float>.SetDefaultValue(_setting, 0.7f);
		}

		public void SetMixer(AudioMixer mixer)
		{
			_mixer = mixer;
			_mixerSet = true;
			SetFloat(UserSetting<float>.Get(_setting));
		}

		private void SetFloat(float vol)
		{
			if (_mixerSet)
			{
				vol = Mathf.Clamp01(vol);
				float value = ((!(vol > 0f)) ? (-144f) : (20f * Mathf.Log10(vol)));
				_mixer.SetFloat(_floatName, value);
			}
		}
	}

	public enum VolumeSliderSetting
	{
		Master,
		VoiceChat,
		SoundEffects,
		MenuMusic,
		MenuUI,
		Scp127Voice,
		Scp3114Voice
	}

	[SerializeField]
	private AudioMixer _mixer;

	private const float DefaultSliderVal = 0.7f;

	private static readonly AudioSlider[] Sliders = new AudioSlider[5]
	{
		new AudioSlider(VolumeSliderSetting.Master, "AudioSettings_Master"),
		new AudioSlider(VolumeSliderSetting.VoiceChat, "AudioSettings_VoiceChat"),
		new AudioSlider(VolumeSliderSetting.SoundEffects, "AudioSettings_Effects"),
		new AudioSlider(VolumeSliderSetting.MenuMusic, "AudioSettings_MenuMusic"),
		new AudioSlider(VolumeSliderSetting.MenuUI, "AudioSettings_Interface")
	};

	private void Start()
	{
		Sliders.ForEach(delegate(AudioSlider x)
		{
			x.SetMixer(_mixer);
		});
	}

	[RuntimeInitializeOnLoadMethod]
	private static void ApplyOtherDefaults()
	{
		UserSetting<float>.SetDefaultValue(VolumeSliderSetting.Scp127Voice, 1f);
		UserSetting<float>.SetDefaultValue(VolumeSliderSetting.Scp3114Voice, 1f);
	}
}
