using System;
using AudioPooling;
using InventorySystem.Items.Usables.Scp1344;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;
using PlayerRoles.Visibility;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects;

public class Scp1344 : StatusEffectBase, ICustomRADisplay
{
	public const InvisibilityFlags BypassFlags = (InvisibilityFlags)3u;

	[SerializeField]
	private AudioClip _sfxEnable;

	[SerializeField]
	private AudioClip _sfxEnableNonDiegetic;

	[SerializeField]
	private AudioClip _sfxBuildupDiegetic;

	[SerializeField]
	private Scp1344XrayProviderBase[] _xrayProviders;

	private AudioPoolSession _enableSoundSession;

	private bool _prevVisionActive;

	public override EffectClassification Classification => EffectClassification.Mixed;

	public string DisplayName => "SCP-1344";

	public bool CanBeDisplayed => true;

	public static event Action<ReferenceHub, ReferenceHub> OnPlayerSeen;

	protected override void Start()
	{
		this.DisableEffect();
	}

	protected override void Awake()
	{
		base.Awake();
		Scp1344XrayProviderBase[] xrayProviders = this._xrayProviders;
		for (int i = 0; i < xrayProviders.Length; i++)
		{
			xrayProviders[i].OnInit(this);
		}
	}

	protected override void Enabled()
	{
		if (NetworkServer.active)
		{
			base.Hub.EnableWearables(WearableElements.Scp1344Goggles);
		}
		this.PlaySound();
	}

	protected override void Disabled()
	{
		if (NetworkServer.active)
		{
			base.Hub.DisableWearables(WearableElements.Scp1344Goggles);
		}
		this.UpdateVision(isVisionActive: false);
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		this.UpdateVision(base.IsPOV || NetworkServer.active);
	}

	public override void OnStopSpectating()
	{
		base.OnStopSpectating();
		this.CloseSoundSessions();
		this.UpdateVision(isVisionActive: false);
	}

	public void PlayBuildupSound()
	{
		AudioSourcePoolManager.PlayOnTransform(this._sfxBuildupDiegetic, base.Hub.transform);
	}

	private void CloseSoundSessions()
	{
		if (this._enableSoundSession.SameSession)
		{
			this._enableSoundSession.Source.Stop();
		}
	}

	private void PlaySound()
	{
		AudioSourcePoolManager.PlayOnTransform(this._sfxEnable, base.Hub.transform);
		if (base.IsLocalPlayer || base.IsSpectated)
		{
			this._enableSoundSession = new AudioPoolSession(AudioSourcePoolManager.Play2D(this._sfxEnableNonDiegetic, 1f, MixerChannel.NoDucking));
		}
	}

	private void UpdateVision(bool isVisionActive)
	{
		if (isVisionActive != this._prevVisionActive)
		{
			if (isVisionActive)
			{
				this._xrayProviders.ForEach(delegate(Scp1344XrayProviderBase x)
				{
					x.OnVisionEnabled();
				});
			}
			else
			{
				this._xrayProviders.ForEach(delegate(Scp1344XrayProviderBase x)
				{
					x.OnVisionDisabled();
				});
			}
			this._prevVisionActive = isVisionActive;
		}
		if (isVisionActive)
		{
			this._xrayProviders.ForEach(delegate(Scp1344XrayProviderBase x)
			{
				x.OnUpdate();
			});
		}
	}
}
