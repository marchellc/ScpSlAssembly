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

	private static readonly CachedUserSetting<bool> NoiseSuppressionSetting = new CachedUserSetting<bool>(VcAudioSetting.NoiseReduction);

	private static bool MicCaptureDenied => (VoiceChatPrivacySettings.PrivacyFlags & VcPrivacyFlags.AllowMicCapture) == 0;

	private void Awake()
	{
		if (_singleton != null)
		{
			throw new InvalidOperationException("More than one 'VoiceChatMicCapture'' detected on scene!");
		}
		_singleton = this;
		_singletonSet = true;
		try
		{
			_noiseReducer = new NoiseReducer(VoiceChatSettings.NoiseReductionSettings);
		}
		catch (Exception ex)
		{
			_noiseReductionFailed = true;
			Debug.LogWarning("Noise reducer exception: " + ex.Message);
		}
		_noiseReductionBuffer = new float[480];
		_recordBuffer = new PlaybackBuffer();
		_sendBuffer = new PlaybackBuffer();
		_micSource = base.gameObject.AddComponent<AudioSource>();
		_micSource.volume = 0f;
		_micSource.loop = true;
		_micSource.bypassEffects = true;
		_micSource.bypassListenerEffects = true;
		_micSource.bypassReverbZones = true;
		VoiceChatPrivacySettings.OnUserFlagsChanged += OnPrivacySettingsUpdated;
	}

	private void OnDestroy()
	{
		VoiceChatPrivacySettings.OnUserFlagsChanged -= OnPrivacySettingsUpdated;
		StopAllMicrophones();
		_singletonSet = false;
	}

	private void Update()
	{
		if (UpdateRecording(out var loudness, out var isSpeaking))
		{
			VoiceChatMicrophoneIndicator.ShowIndicator(isSpeaking, loudness);
		}
	}

	private void OnPrivacySettingsUpdated(ReferenceHub hub)
	{
		if (hub.isLocalPlayer && (!_micStarted || MicCaptureDenied))
		{
			RestartRecording();
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
		_channel = voiceRole.VoiceModule.GetUserInput();
		bool flag = _channel == VoiceChatChannel.None;
		isSpeaking = !flag && MicCaptureDenied;
		if (!_micStarted)
		{
			return true;
		}
		int position = Microphone.GetPosition(_selectedMic);
		_micSource.clip.GetData(_samples, 0);
		int num = position - _lastSample;
		if (num < 0)
		{
			num += _samplesCount;
		}
		if (flag)
		{
			_lastSample = position;
			return true;
		}
		for (int i = 0; i < num; i++)
		{
			float num2 = _samples[(_lastSample + i) % _samplesCount];
			if (num2 > loudness)
			{
				loudness = num2;
			}
			_recordBuffer.Write(num2);
		}
		_lastSample = position;
		int num3 = 480;
		while (_recordBuffer.Length >= num3)
		{
			_recordBuffer.ReadTo(_noiseReductionBuffer, num3, 0L);
			if (NoiseSuppressionSetting.Value && !_noiseReductionFailed)
			{
				_noiseReducer.ReduceNoise(_noiseReductionBuffer);
			}
			_sendBuffer.Write(_noiseReductionBuffer, num3);
			VoiceTransceiver.ClientSendData(_sendBuffer, _channel);
		}
		isSpeaking = true;
		return num > 0;
	}

	public static void StopAllMicrophones()
	{
		if (!_singletonSet || !_singleton._micStarted)
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
		_singleton._micStarted = false;
	}

	public static void StartRecording()
	{
		if (_singletonSet && !_singleton._micStarted)
		{
			RestartRecording();
		}
	}

	public static void RestartRecording()
	{
		if (!_singletonSet || !VoiceChatMicrophoneSelector.TryGetPreferredMicrophone(out var mic))
		{
			return;
		}
		if (MicCaptureDenied)
		{
			StopAllMicrophones();
			return;
		}
		if (_singleton._micStarted && _selectedMic != mic)
		{
			StopAllMicrophones();
			_selectedMic = mic;
		}
		AudioSource micSource = _singleton._micSource;
		micSource.clip = Microphone.Start(_selectedMic, loop: true, 1, 48000);
		if (micSource.clip == null)
		{
			_singleton._micStarted = false;
			Debug.LogError("Microphone '" + _selectedMic + "' failed to start recording.");
			return;
		}
		_singleton._recordBuffer.Clear();
		_singleton._sendBuffer.Clear();
		_singleton._micStarted = true;
		int samples = micSource.clip.samples;
		_singleton._lastSample = 0;
		if (samples != _singleton._samplesCount)
		{
			_singleton._samplesCount = samples;
			_singleton._samples = new float[samples];
		}
		micSource.Play();
	}

	public static VoiceChatChannel GetCurrentChannel()
	{
		if (!_singletonSet)
		{
			return VoiceChatChannel.None;
		}
		return _singleton._channel;
	}
}
