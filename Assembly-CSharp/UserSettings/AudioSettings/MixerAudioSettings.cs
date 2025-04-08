using System;
using UnityEngine;
using UnityEngine.Audio;

namespace UserSettings.AudioSettings
{
	public class MixerAudioSettings : MonoBehaviour
	{
		private void Start()
		{
			MixerAudioSettings.Sliders.ForEach(delegate(MixerAudioSettings.AudioSlider x)
			{
				x.SetMixer(this._mixer);
			});
		}

		[SerializeField]
		private AudioMixer _mixer;

		private const float DefaultSliderVal = 0.7f;

		private static readonly MixerAudioSettings.AudioSlider[] Sliders = new MixerAudioSettings.AudioSlider[]
		{
			new MixerAudioSettings.AudioSlider(MixerAudioSettings.VolumeSliderSetting.Master, "AudioSettings_Master"),
			new MixerAudioSettings.AudioSlider(MixerAudioSettings.VolumeSliderSetting.VoiceChat, "AudioSettings_VoiceChat"),
			new MixerAudioSettings.AudioSlider(MixerAudioSettings.VolumeSliderSetting.SoundEffects, "AudioSettings_Effects"),
			new MixerAudioSettings.AudioSlider(MixerAudioSettings.VolumeSliderSetting.MenuMusic, "AudioSettings_MenuMusic"),
			new MixerAudioSettings.AudioSlider(MixerAudioSettings.VolumeSliderSetting.MenuUI, "AudioSettings_Interface")
		};

		private class AudioSlider
		{
			public AudioSlider(MixerAudioSettings.VolumeSliderSetting setting, string floatName)
			{
				this._setting = setting;
				this._floatName = floatName;
				UserSetting<float>.AddListener<MixerAudioSettings.VolumeSliderSetting>(this._setting, new Action<float>(this.SetFloat));
				UserSetting<float>.SetDefaultValue<MixerAudioSettings.VolumeSliderSetting>(this._setting, 0.7f);
			}

			public void SetMixer(AudioMixer mixer)
			{
				this._mixer = mixer;
				this._mixerSet = true;
				this.SetFloat(UserSetting<float>.Get<MixerAudioSettings.VolumeSliderSetting>(this._setting));
			}

			private void SetFloat(float vol)
			{
				if (!this._mixerSet)
				{
					return;
				}
				vol = Mathf.Clamp01(vol);
				float num;
				if (vol > 0f)
				{
					num = 20f * Mathf.Log10(vol);
				}
				else
				{
					num = -144f;
				}
				this._mixer.SetFloat(this._floatName, num);
			}

			private readonly MixerAudioSettings.VolumeSliderSetting _setting;

			private readonly string _floatName;

			private AudioMixer _mixer;

			private bool _mixerSet;
		}

		public enum VolumeSliderSetting
		{
			Master,
			VoiceChat,
			SoundEffects,
			MenuMusic,
			MenuUI
		}
	}
}
