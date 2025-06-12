using System;
using AudioPooling;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

[Serializable]
public class Scp939LungeAudio
{
	private Transform _t;

	[SerializeField]
	private AudioClip _harsh;

	[SerializeField]
	private AudioClip _land;

	[SerializeField]
	private AudioClip[] _hits;

	[SerializeField]
	private AudioClip _launch;

	public void Init(Scp939LungeAbility lunge)
	{
		lunge.OnStateChanged += OnStateChanged;
		this._t = lunge.transform;
	}

	private void OnStateChanged(Scp939LungeState state)
	{
		switch (state)
		{
		case Scp939LungeState.LandHit:
			this.Play(this._hits.RandomItem(), 25f);
			break;
		case Scp939LungeState.LandHarsh:
			this.Play(this._harsh, 12.5f);
			break;
		case Scp939LungeState.Triggered:
			this.Play(this._launch, 25f);
			return;
		case Scp939LungeState.None:
			return;
		}
		this.Play(this._land, 25f);
	}

	private void Play(AudioClip clip, float dis)
	{
		AudioSourcePoolManager.PlayOnTransform(clip, this._t, dis);
	}
}
