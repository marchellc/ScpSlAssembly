using System;
using System.Collections.Generic;
using AudioPooling;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules;

public class AudioController : MonoBehaviour, ISoundEmittingItem
{
	[Serializable]
	private struct FiringClipSet
	{
		public MicroHidFiringMode FiringMode;

		public AudioClip WindUpClip;

		public AudioClip StopFiringClip;

		public AudioClip FireClip;

		public AudioClip AbortClip;

		public AnimationCurve AbortStartTimeOverElapsed;
	}

	private bool _cacheSet;

	private bool? _last3d;

	private MicroHidPhase _lastPhase;

	private Transform _cachedTransform;

	private bool _allSilent;

	private FiringClipSet _lastClipSet;

	private float _crossfadeSpeed;

	private readonly List<AudioPoolSession> _activeSessions = new List<AudioPoolSession>();

	[SerializeField]
	private AudioSource _windupSource;

	[SerializeField]
	private AudioSource _winddownSource;

	[SerializeField]
	private AudioSource _firingSource;

	[SerializeField]
	private FiringClipSet[] _clipSets;

	public List<AudioPoolSession> ActiveSessions
	{
		get
		{
			for (int num = this._activeSessions.Count - 1; num >= 0; num--)
			{
				if (!this._activeSessions[num].SameSession)
				{
					this._activeSessions.RemoveAt(num);
				}
			}
			return this._activeSessions;
		}
	}

	public ushort Serial { get; set; }

	public bool Idle
	{
		get
		{
			if (this._lastPhase != MicroHidPhase.Standby)
			{
				return false;
			}
			if (!this._allSilent)
			{
				return false;
			}
			if (this.ActiveSessions.Count > 0)
			{
				return false;
			}
			return true;
		}
	}

	public Transform FastTr
	{
		get
		{
			this.ValidateCache();
			return this._cachedTransform;
		}
	}

	private void ValidateCache()
	{
		if (!this._cacheSet)
		{
			this._cachedTransform = base.transform;
			this._last3d = null;
			this._cacheSet = true;
		}
	}

	private void AdjustVolume(AudioSource src, bool enable, float speed)
	{
		float target = (enable ? 1 : 0);
		float maxDelta = speed * Time.deltaTime;
		src.volume = Mathf.MoveTowards(src.volume, target, maxDelta);
	}

	private void AdjustVolumeFast(AudioSource src, bool enable)
	{
		this.AdjustVolume(src, enable, 11f);
	}

	private void AdjustVolumeSlow(AudioSource src, bool enable)
	{
		this.AdjustVolume(src, enable, 2f);
	}

	private void AdjustVolumeInstant(AudioSource src, bool enable)
	{
		src.volume = (enable ? 1 : 0);
	}

	private void OnDestroy()
	{
		AudioManagerModule.RegisterDestroyed(this);
	}

	public AudioPoolSession PlayOneShot(AudioClip clip, float range = 10f, MixerChannel mixer = MixerChannel.NoDucking)
	{
		AudioPoolSession audioPoolSession = new AudioPoolSession(AudioSourcePoolManager.PlayOnTransform(clip, this.FastTr, range, 1f, FalloffType.Exponential, mixer));
		this.ActiveSessions.Add(audioPoolSession);
		this.UpdateSourcePosition();
		return audioPoolSession;
	}

	public void UpdateAudio(MicroHidPhase phase)
	{
		this.UpdateConditionally(phase);
		this._lastPhase = phase;
	}

	private void UpdateConditionally(MicroHidPhase phase)
	{
		if (this.Idle)
		{
			return;
		}
		this.UpdateSourcePosition();
		if (phase == MicroHidPhase.Standby)
		{
			this.UpdateStandby();
			return;
		}
		bool flag = this._lastPhase != phase;
		if (!flag || this.TryFindClipSet(out this._lastClipSet))
		{
			switch (phase)
			{
			case MicroHidPhase.WindingUp:
				this.UpdateWindUp(flag);
				break;
			case MicroHidPhase.WoundUpSustain:
				this.UpdateWindUp(changed: false);
				break;
			case MicroHidPhase.WindingDown:
				this.UpdateWindDown(flag);
				break;
			case MicroHidPhase.Firing:
				this.UpdateFiring(flag);
				break;
			}
			this._allSilent = false;
		}
	}

	private void Set3d(bool is3D)
	{
		if (is3D != this._last3d)
		{
			this._windupSource.SetSpace(is3D);
			this._winddownSource.SetSpace(is3D);
			this._firingSource.SetSpace(is3D);
			this._last3d = is3D;
		}
	}

	private void UpdateSourcePosition()
	{
		ReferenceHub hub;
		if (MicroHIDPickup.PickupsBySerial.TryGetValue(this.Serial, out var value))
		{
			this.FastTr.position = value.Position;
			this.Set3d(is3D: true);
		}
		else if (InventoryExtensions.TryGetHubHoldingSerial(this.Serial, out hub))
		{
			this.Set3d(!hub.isLocalPlayer && !hub.IsLocallySpectated());
			this.FastTr.position = hub.GetPosition();
		}
	}

	private bool TryFindClipSet(out FiringClipSet clipSet)
	{
		MicroHidFiringMode firingMode = CycleSyncModule.GetFiringMode(this.Serial);
		FiringClipSet[] clipSets = this._clipSets;
		for (int i = 0; i < clipSets.Length; i++)
		{
			FiringClipSet firingClipSet = clipSets[i];
			if (firingClipSet.FiringMode == firingMode)
			{
				clipSet = firingClipSet;
				return true;
			}
		}
		clipSet = default(FiringClipSet);
		return false;
	}

	private void UpdateStandby()
	{
		if (!this._allSilent)
		{
			this.AdjustVolumeFast(this._firingSource, enable: false);
			if (this._crossfadeSpeed > 0f)
			{
				this.AdjustVolume(this._windupSource, enable: false, this._crossfadeSpeed);
				this.AdjustVolume(this._winddownSource, enable: true, this._crossfadeSpeed);
			}
			else
			{
				this.AdjustVolumeFast(this._windupSource, enable: false);
			}
			this._allSilent = this._winddownSource.volume == 0f && this._windupSource.volume == 0f && this._firingSource.volume == 0f;
		}
	}

	private void UpdateWindUp(bool changed)
	{
		if (changed)
		{
			this._windupSource.clip = this._lastClipSet.WindUpClip;
			this.AdjustVolumeInstant(this._windupSource, enable: true);
			this._windupSource.Play();
		}
		this.AdjustVolumeSlow(this._winddownSource, enable: false);
		this.AdjustVolumeFast(this._firingSource, enable: false);
	}

	private void UpdateWindDown(bool changed)
	{
		if (!changed)
		{
			this.AdjustVolume(this._windupSource, enable: false, this._crossfadeSpeed);
			this.AdjustVolume(this._winddownSource, enable: true, this._crossfadeSpeed);
			this.AdjustVolumeSlow(this._firingSource, enable: false);
			return;
		}
		float num;
		AudioClip audioClip;
		if (this._lastPhase == MicroHidPhase.Firing)
		{
			num = 0f;
			audioClip = this._lastClipSet.StopFiringClip;
			this._crossfadeSpeed = 4f;
		}
		else
		{
			audioClip = this._lastClipSet.AbortClip;
			float length = this._windupSource.clip.length;
			float time = this._windupSource.time;
			float num2 = length - time;
			if (num2 > 0.01f)
			{
				this._crossfadeSpeed = Mathf.Max(4f, 1f / num2);
				num = this._lastClipSet.AbortStartTimeOverElapsed.Evaluate(time);
				this.AdjustVolumeInstant(this._winddownSource, enable: false);
			}
			else
			{
				num = 0f;
				this.AdjustVolumeInstant(this._winddownSource, enable: true);
				this.AdjustVolumeInstant(this._windupSource, enable: false);
			}
		}
		if (num <= audioClip.length)
		{
			this._winddownSource.clip = audioClip;
			this._winddownSource.Play();
			this._winddownSource.time = Mathf.Max(0f, num);
		}
		else
		{
			this._winddownSource.Stop();
		}
		this.UpdateWindDown(changed: false);
	}

	private void UpdateFiring(bool changed)
	{
		if (changed)
		{
			this._firingSource.clip = this._lastClipSet.FireClip;
			this._firingSource.Play();
		}
		this.AdjustVolumeSlow(this._windupSource, enable: false);
		this.AdjustVolumeSlow(this._winddownSource, enable: false);
		this.AdjustVolumeInstant(this._firingSource, enable: true);
	}

	public bool ServerTryGetSoundEmissionRange(out float range)
	{
		switch (this._lastPhase)
		{
		case MicroHidPhase.WindingUp:
		case MicroHidPhase.WoundUpSustain:
			range = this._windupSource.maxDistance;
			return true;
		case MicroHidPhase.WindingDown:
			range = this._winddownSource.maxDistance;
			return true;
		case MicroHidPhase.Firing:
			range = this._firingSource.maxDistance;
			return true;
		default:
			range = 0f;
			return false;
		}
	}
}
