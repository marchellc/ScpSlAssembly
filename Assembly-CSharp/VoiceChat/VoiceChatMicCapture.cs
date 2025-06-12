using System;
using PlayerRoles.Voice;
using UnityEngine;
using UserSettings;
using UserSettings.AudioSettings;
using VoiceChat.CaressNoiseReduction;
using VoiceChat.Networking;

namespace VoiceChat;

public class VoiceChatMicCapture : MonoBehaviour
{
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

	private static readonly CachedUserSetting<bool> NoiseSuppressionSetting = new CachedUserSetting<bool>(OtherAudioSetting.NoiseReduction);

	private static bool MicCaptureDenied => (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowMicCapture) == 0;

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
		this._recordBuffer = new PlaybackBuffer();
		this._sendBuffer = new PlaybackBuffer();
		this._micSource = base.gameObject.AddComponent<AudioSource>();
		this._micSource.volume = 0f;
		this._micSource.loop = true;
		this._micSource.bypassEffects = true;
		this._micSource.bypassListenerEffects = true;
		this._micSource.bypassReverbZones = true;
		VoiceChatPrivacySettings.OnUserFlagsChanged += OnPrivacySettingsUpdated;
	}

	private void OnDestroy()
	{
		VoiceChatPrivacySettings.OnUserFlagsChanged -= OnPrivacySettingsUpdated;
		VoiceChatMicCapture.StopAllMicrophones();
		VoiceChatMicCapture._singletonSet = false;
	}

	private void Update()
	{
		if (this.UpdateRecording(out var loudness, out var isSpeaking))
		{
			VoiceChatMicrophoneIndicator.ShowIndicator(isSpeaking, loudness);
		}
	}

	private void OnPrivacySettingsUpdated(ReferenceHub hub)
	{
		if (hub.isLocalPlayer && (!this._micStarted || VoiceChatMicCapture.MicCaptureDenied))
		{
			VoiceChatMicCapture.RestartRecording();
		}
	}

	private bool UpdateRecording(out float loudness, out bool isSpeaking)
	{
		loudness = 0f;
		isSpeaking = false;
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return true;
		}
		if (!(hub.roleManager.CurrentRole is IVoiceRole voiceRole))
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
			this._recordBuffer.ReadTo(this._noiseReductionBuffer, num3, 0L);
			if (VoiceChatMicCapture.NoiseSuppressionSetting.Value && !this._noiseReductionFailed)
			{
				this._noiseReducer.ReduceNoise(this._noiseReductionBuffer);
			}
			this._sendBuffer.Write(this._noiseReductionBuffer, num3);
			VoiceTransceiver.ClientSendData(this._sendBuffer, this._channel);
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
		string[] devices = Microphone.devices;
		foreach (string deviceName in devices)
		{
			if (Microphone.IsRecording(deviceName))
			{
				Microphone.End(deviceName);
			}
		}
		VoiceChatMicCapture._singleton._micStarted = false;
	}

	public static void StartRecording()
	{
		if (VoiceChatMicCapture._singletonSet && !VoiceChatMicCapture._singleton._micStarted)
		{
			VoiceChatMicCapture.RestartRecording();
		}
	}

	public static void RestartRecording()
	{
		if (!VoiceChatMicCapture._singletonSet || !VoiceChatMicrophoneSelector.TryGetPreferredMicrophone(out var mic))
		{
			return;
		}
		if (VoiceChatMicCapture.MicCaptureDenied)
		{
			VoiceChatMicCapture.StopAllMicrophones();
			return;
		}
		if (VoiceChatMicCapture._singleton._micStarted && VoiceChatMicCapture._selectedMic != mic)
		{
			VoiceChatMicCapture.StopAllMicrophones();
			VoiceChatMicCapture._selectedMic = mic;
		}
		AudioSource micSource = VoiceChatMicCapture._singleton._micSource;
		micSource.clip = Microphone.Start(VoiceChatMicCapture._selectedMic, loop: true, 1, 48000);
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
}
