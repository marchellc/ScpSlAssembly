using System;
using PlayerRoles.Voice;
using UnityEngine;
using UserSettings;
using UserSettings.AudioSettings;
using VoiceChat.CaressNoiseReduction;
using VoiceChat.Networking;

namespace VoiceChat
{
	public class VoiceChatMicCapture : MonoBehaviour
	{
		private static bool MicCaptureDenied
		{
			get
			{
				return (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowMicCapture) == VcPrivacyFlags.None;
			}
		}

		private void Awake()
		{
			if (VoiceChatMicCapture._singleton != null)
			{
				throw new InvalidOperationException("More than one 'VoiceChatMicCapture'' detected on scene!");
			}
			VoiceChatMicCapture._singleton = this;
			VoiceChatMicCapture._singletonSet = true;
			try
			{
				this._noiseReducer = new NoiseReducer(VoiceChatSettings.NoiseReductionSettings);
			}
			catch (Exception ex)
			{
				this._noiseReductionFailed = true;
				Debug.LogWarning("Noise reducer exception: " + ex.Message);
			}
			this._noiseReductionBuffer = new float[480];
			this._recordBuffer = new PlaybackBuffer(24000, false);
			this._sendBuffer = new PlaybackBuffer(24000, false);
			this._micSource = base.gameObject.AddComponent<AudioSource>();
			this._micSource.volume = 0f;
			this._micSource.loop = true;
			this._micSource.bypassEffects = true;
			this._micSource.bypassListenerEffects = true;
			this._micSource.bypassReverbZones = true;
			VoiceChatPrivacySettings.OnUserFlagsChanged += this.OnPrivacySettingsUpdated;
		}

		private void OnDestroy()
		{
			VoiceChatPrivacySettings.OnUserFlagsChanged -= this.OnPrivacySettingsUpdated;
			VoiceChatMicCapture.StopAllMicrophones();
			VoiceChatMicCapture._singletonSet = false;
		}

		private void Update()
		{
			float num;
			bool flag;
			if (!this.UpdateRecording(out num, out flag))
			{
				return;
			}
			VoiceChatMicrophoneIndicator.ShowIndicator(flag, num);
		}

		private void OnPrivacySettingsUpdated(ReferenceHub hub)
		{
			if (!hub.isLocalPlayer)
			{
				return;
			}
			if (this._micStarted && !VoiceChatMicCapture.MicCaptureDenied)
			{
				return;
			}
			VoiceChatMicCapture.RestartRecording();
		}

		private bool UpdateRecording(out float loudness, out bool isSpeaking)
		{
			loudness = 0f;
			isSpeaking = false;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return true;
			}
			IVoiceRole voiceRole = referenceHub.roleManager.CurrentRole as IVoiceRole;
			if (voiceRole == null)
			{
				return true;
			}
			this._channel = voiceRole.VoiceModule.GetUserInput();
			bool flag = this._channel == VoiceChatChannel.None;
			isSpeaking = !flag && VoiceChatMicCapture.MicCaptureDenied;
			if (!this._micStarted)
			{
				return true;
			}
			int position = Microphone.GetPosition(VoiceChatMicCapture._selectedMic);
			this._micSource.clip.GetData(this._samples, 0);
			int num = position - this._lastSample;
			if (num < 0)
			{
				num += this._samplesCount;
			}
			if (flag)
			{
				this._lastSample = position;
				return true;
			}
			for (int i = 0; i < num; i++)
			{
				float num2 = this._samples[(this._lastSample + i) % this._samplesCount];
				if (num2 > loudness)
				{
					loudness = num2;
				}
				this._recordBuffer.Write(num2);
			}
			this._lastSample = position;
			int num3 = 480;
			while (this._recordBuffer.Length >= num3)
			{
				this._recordBuffer.ReadTo(this._noiseReductionBuffer, (long)num3, 0L);
				if (VoiceChatMicCapture.NoiseSuppressionSetting.Value && !this._noiseReductionFailed)
				{
					this._noiseReducer.ReduceNoise(this._noiseReductionBuffer);
				}
				this._sendBuffer.Write(this._noiseReductionBuffer, num3);
				VoiceTransceiver.ClientSendData(this._sendBuffer, this._channel, 0);
			}
			isSpeaking = true;
			return num > 0;
		}

		public static void StopAllMicrophones()
		{
			if (!VoiceChatMicCapture._singletonSet || !VoiceChatMicCapture._singleton._micStarted)
			{
				return;
			}
			foreach (string text in Microphone.devices)
			{
				if (Microphone.IsRecording(text))
				{
					Microphone.End(text);
				}
			}
			VoiceChatMicCapture._singleton._micStarted = false;
		}

		public static void StartRecording()
		{
			if (!VoiceChatMicCapture._singletonSet || VoiceChatMicCapture._singleton._micStarted)
			{
				return;
			}
			VoiceChatMicCapture.RestartRecording();
		}

		public static void RestartRecording()
		{
			string text;
			if (!VoiceChatMicCapture._singletonSet || !VoiceChatMicrophoneSelector.TryGetPreferredMicrophone(out text))
			{
				return;
			}
			if (VoiceChatMicCapture.MicCaptureDenied)
			{
				VoiceChatMicCapture.StopAllMicrophones();
				return;
			}
			if (VoiceChatMicCapture._singleton._micStarted && VoiceChatMicCapture._selectedMic != text)
			{
				VoiceChatMicCapture.StopAllMicrophones();
				VoiceChatMicCapture._selectedMic = text;
			}
			AudioSource micSource = VoiceChatMicCapture._singleton._micSource;
			micSource.clip = Microphone.Start(VoiceChatMicCapture._selectedMic, true, 1, 48000);
			if (micSource.clip == null)
			{
				VoiceChatMicCapture._singleton._micStarted = false;
				Debug.LogError("Microphone '" + VoiceChatMicCapture._selectedMic + "' failed to start recording.");
				return;
			}
			VoiceChatMicCapture._singleton._recordBuffer.Clear();
			VoiceChatMicCapture._singleton._sendBuffer.Clear();
			VoiceChatMicCapture._singleton._micStarted = true;
			int samples = micSource.clip.samples;
			VoiceChatMicCapture._singleton._lastSample = 0;
			if (samples != VoiceChatMicCapture._singleton._samplesCount)
			{
				VoiceChatMicCapture._singleton._samplesCount = samples;
				VoiceChatMicCapture._singleton._samples = new float[samples];
			}
			micSource.Play();
		}

		public static VoiceChatChannel GetCurrentChannel()
		{
			if (!VoiceChatMicCapture._singletonSet)
			{
				return VoiceChatChannel.None;
			}
			return VoiceChatMicCapture._singleton._channel;
		}

		private static VoiceChatMicCapture _singleton;

		private static bool _singletonSet;

		private static string _selectedMic;

		private NoiseReducer _noiseReducer;

		private PlaybackBuffer _recordBuffer;

		private PlaybackBuffer _sendBuffer;

		private AudioSource _micSource;

		private VoiceChatChannel _channel;

		private float[] _noiseReductionBuffer;

		private float[] _samples;

		private int _lastSample;

		private int _samplesCount;

		private bool _micStarted;

		private bool _noiseReductionFailed;

		private static readonly CachedUserSetting<bool> NoiseSuppressionSetting = new CachedUserSetting<bool>(VcAudioSetting.NoiseReduction);
	}
}
