using System;
using AudioPooling;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079DoorWhir
{
	private readonly Scp079DoorLockChanger _lockChanger;

	private readonly Scp079AuxManager _auxManager;

	private readonly DoorVariant _door;

	private readonly AudioSource _whirSrc;

	private bool _active;

	private bool _deactivating;

	private float _startAux;

	private const float WhirDist = 15f;

	private const float VolumeAdjustSpeed = 0.7f;

	private const float PitchLerpSpeed = 2f;

	private const float ShutdownPitch = 0.5f;

	private const float StartPitch = 0.9f;

	private const float FinalPitch = 1.8f;

	private const float MinStartAux = 5f;

	private bool Valid
	{
		get
		{
			if (this._door != null && this._lockChanger.LockedDoor == this._door)
			{
				return !this._lockChanger.Role.Pooled;
			}
			return false;
		}
	}

	public Scp079DoorWhir(Scp079Role scp079, AudioClip whirSound)
	{
		scp079.SubroutineModule.TryGetSubroutine<Scp079DoorLockChanger>(out this._lockChanger);
		scp079.SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out this._auxManager);
		this._door = this._lockChanger.LockedDoor;
		StaticUnityMethods.OnUpdate += OnUpdate;
		this._whirSrc = AudioSourcePoolManager.CreateNewSource().Source;
		AudioSourcePoolManager.ApplyStandardSettings(this._whirSrc, whirSound, FalloffType.Exponential, MixerChannel.NoDucking, 1f, 15f);
		this._whirSrc.transform.position = this._door.transform.position;
		this._whirSrc.loop = true;
		this._whirSrc.volume = 0f;
		this._whirSrc.Play();
		this._active = true;
		this._startAux = 5f;
	}

	private void OnUpdate()
	{
		try
		{
			this.UpdateAudioSource();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void UpdateAudioSource()
	{
		if (this._deactivating || !this.Valid)
		{
			this._deactivating = true;
			this.MuteSource();
			return;
		}
		if (this._door.TargetState)
		{
			this.MuteSource();
			return;
		}
		if (this._whirSrc.volume == 0f)
		{
			this._startAux = Mathf.Max(this._auxManager.CurrentAuxFloored, 5f);
		}
		float b = Mathf.Lerp(1.8f, 0.9f, (float)this._auxManager.CurrentAuxFloored / this._startAux);
		this._whirSrc.pitch = Mathf.Lerp(this._whirSrc.pitch, b, Time.deltaTime * 2f);
		this._whirSrc.volume += 0.7f * Time.deltaTime;
	}

	private void MuteSource()
	{
		if (this._whirSrc == null)
		{
			this.Destruct();
			return;
		}
		this._whirSrc.volume -= 0.7f * Time.deltaTime;
		this._whirSrc.pitch = Mathf.Lerp(this._whirSrc.pitch, 0.5f, Time.deltaTime * 2f);
		if (this._deactivating && this._whirSrc.volume <= 0f)
		{
			this.Destruct();
		}
	}

	private void Destruct()
	{
		if (this._active)
		{
			this._active = false;
			StaticUnityMethods.OnUpdate -= OnUpdate;
			if (!(this._whirSrc == null) && !(this._whirSrc.gameObject == null))
			{
				UnityEngine.Object.Destroy(this._whirSrc.gameObject);
			}
		}
	}

	~Scp079DoorWhir()
	{
		this.Destruct();
	}
}
