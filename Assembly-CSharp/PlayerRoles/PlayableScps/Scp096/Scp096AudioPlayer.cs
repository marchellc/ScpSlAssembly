using System;
using System.Collections.Generic;
using AudioPooling;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096AudioPlayer : StandardSubroutine<Scp096Role>
{
	[Serializable]
	public class Scp096StateAudio
	{
		public AudioClip Audio;

		public Scp096RageState State;

		public FalloffType Falloff;

		public float MaxDistance;
	}

	[SerializeField]
	private AudioSource _rageStatesSource;

	[SerializeField]
	private AudioSource _tryNotToCrySource;

	[SerializeField]
	private float _volumeAdjustLerp;

	[SerializeField]
	private CurvePreset[] _curves;

	[SerializeField]
	private Scp096StateAudio[] _rageStatesAudioClips;

	[SerializeField]
	private AudioClip[] _lethalClips;

	[SerializeField]
	private AudioClip[] _nonLethalClips;

	[SerializeField]
	private float _lethalDistance;

	[SerializeField]
	private float _nonLethalDistance;

	[SerializeField]
	private float _pitchRandomization;

	private static bool _soundsDictionarized = false;

	private Scp096HitResult _syncHitSound;

	private static readonly Dictionary<Scp096RageState, Scp096StateAudio> AudioStates = new Dictionary<Scp096RageState, Scp096StateAudio>();

	private static readonly Dictionary<FalloffType, CurvePreset> Curves = new Dictionary<FalloffType, CurvePreset>();

	private void Update()
	{
		bool flag = base.CastRole.IsAbilityState(Scp096AbilityState.TryingNotToCry);
		float num = Mathf.Lerp(this._tryNotToCrySource.volume, flag ? 1 : 0, Time.deltaTime * this._volumeAdjustLerp);
		this._tryNotToCrySource.volume = num;
		this._rageStatesSource.volume = 1f - num;
	}

	protected override void Awake()
	{
		base.Awake();
		base.CastRole.StateController.OnRageUpdate += SetAudioState;
		if (!Scp096AudioPlayer._soundsDictionarized)
		{
			Scp096AudioPlayer.Curves.FromArray(this._curves, (CurvePreset x) => x.Type);
			Scp096AudioPlayer.AudioStates.FromArray(this._rageStatesAudioClips, (Scp096StateAudio x) => x.State);
			Scp096AudioPlayer._soundsDictionarized = true;
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this.SetAudioState(base.CastRole.StateController.RageState);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._tryNotToCrySource.volume = 0f;
		this._rageStatesSource.volume = 0f;
	}

	public void Play(AudioClip clip, FalloffType falloff = FalloffType.Linear, float maxDistance = -1f)
	{
		if (Scp096AudioPlayer.Curves.TryGetValue(falloff, out var value))
		{
			this._rageStatesSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, value.FalloffCurve);
			if (maxDistance > 0f)
			{
				this._rageStatesSource.maxDistance = maxDistance;
			}
			this._rageStatesSource.clip = clip;
			this._rageStatesSource.Play();
		}
	}

	public void SetAudioState(Scp096RageState state)
	{
		if (Scp096AudioPlayer.TryGetAudioForState(state, out var stateAudio) && !(this._rageStatesSource.clip == stateAudio.Audio))
		{
			this.Play(stateAudio.Audio, stateAudio.Falloff, stateAudio.MaxDistance);
		}
	}

	public void Stop()
	{
		if (this._rageStatesSource.isPlaying)
		{
			this._rageStatesSource.Stop();
		}
		this._rageStatesSource.clip = null;
	}

	public void ServerPlayAttack(Scp096HitResult hitRes)
	{
		this._syncHitSound = hitRes;
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._syncHitSound);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		Scp096HitResult scp096HitResult = (Scp096HitResult)reader.ReadByte();
		if ((scp096HitResult & Scp096HitResult.Human) != Scp096HitResult.None)
		{
			bool num = (scp096HitResult & Scp096HitResult.Lethal) == Scp096HitResult.Lethal;
			float maxDistance = (num ? this._lethalDistance : this._nonLethalDistance);
			AudioClip sound = (num ? this._lethalClips : this._nonLethalClips).RandomItem();
			MixerChannel channel = (num ? MixerChannel.NoDucking : MixerChannel.DefaultSfx);
			float pitchScale = UnityEngine.Random.Range(1f - this._pitchRandomization, 1f + this._pitchRandomization);
			if (base.Owner.isLocalPlayer)
			{
				AudioSourcePoolManager.Play2D(sound, 1f, channel, pitchScale);
			}
			else
			{
				AudioSourcePoolManager.PlayOnTransform(sound, base.transform, maxDistance, 1f, FalloffType.Exponential, channel, pitchScale);
			}
		}
	}

	public static bool TryGetAudioForState(Scp096RageState state, out Scp096StateAudio stateAudio)
	{
		return Scp096AudioPlayer.AudioStates.TryGetValue(state, out stateAudio);
	}
}
