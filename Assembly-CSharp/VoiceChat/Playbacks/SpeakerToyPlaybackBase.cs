using System.Collections.Generic;
using UnityEngine;
using VoiceChat.Codec;
using VoiceChat.Networking;

namespace VoiceChat.Playbacks;

public class SpeakerToyPlaybackBase : VoiceChatPlaybackBase
{
	public static readonly HashSet<SpeakerToyPlaybackBase> AllInstances = new HashSet<SpeakerToyPlaybackBase>();

	private static float[] _receiveBuffer;

	private static bool _receiveBufferSet;

	private OpusDecoder _decoder = new OpusDecoder();

	public byte ControllerId;

	public PlaybackBuffer Buffer;

	public Vector3 LastPosition { get; private set; }

	public bool Culled { get; private set; }

	public override int MaxSamples => this.Buffer.Length;

	public void DecodeSamples(AudioMessage msg)
	{
		if (!SpeakerToyPlaybackBase._receiveBufferSet)
		{
			SpeakerToyPlaybackBase._receiveBufferSet = true;
			SpeakerToyPlaybackBase._receiveBuffer = new float[24000];
		}
		int length = this._decoder.Decode(msg.Data, msg.DataLength, SpeakerToyPlaybackBase._receiveBuffer);
		this.Buffer.Write(SpeakerToyPlaybackBase._receiveBuffer, length);
	}

	protected override void Awake()
	{
		base.Awake();
		this.Buffer = new PlaybackBuffer();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SpeakerToyPlaybackBase.AllInstances.Add(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		SpeakerToyPlaybackBase.AllInstances.Remove(this);
	}

	protected override void Update()
	{
		base.Update();
		this.LastPosition = base.transform.position;
	}

	private void OnDestroy()
	{
		this._decoder.Dispose();
	}

	protected override float ReadSample()
	{
		return this.Buffer.Read();
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnLateUpdate += OnLateUpdate;
	}

	private static void OnLateUpdate()
	{
		if (!MainCameraController.InstanceActive)
		{
			return;
		}
		Vector3 position = MainCameraController.CurrentCamera.position;
		foreach (SpeakerToyPlaybackBase allInstance in SpeakerToyPlaybackBase.AllInstances)
		{
			allInstance.Culled = true;
			SpeakerToyPlaybackBase.ValidatePlayback(position, allInstance);
		}
	}

	private static void ValidatePlayback(Vector3 cameraPosition, SpeakerToyPlaybackBase playback)
	{
		float sqrMagnitude = (playback.LastPosition - cameraPosition).sqrMagnitude;
		float maxDistance = playback.Source.maxDistance;
		if (!(sqrMagnitude > maxDistance * maxDistance))
		{
			playback.Culled = false;
		}
	}
}
