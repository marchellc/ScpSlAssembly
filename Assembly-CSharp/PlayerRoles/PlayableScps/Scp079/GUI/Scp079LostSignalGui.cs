using System;
using AudioPooling;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079LostSignalGui : Scp079GuiElementBase
{
	[Serializable]
	private struct ZoneClip
	{
		public FacilityZone Zone;

		public AudioClip Clip;
	}

	[SerializeField]
	private GameObject _rootObj;

	[SerializeField]
	private AudioSource _loopSource;

	[SerializeField]
	private TextMeshProUGUI _etaText;

	[SerializeField]
	private ZoneClip[] _zoneStarts;

	[SerializeField]
	private AudioClip _fallbackStart;

	[SerializeField]
	private ZoneClip[] _zoneLoops;

	[SerializeField]
	private AudioClip _fallbackLoop;

	private Scp079LostSignalHandler _handler;

	private Scp079CurrentCameraSync _curCamSync;

	private string _textFormat;

	internal override void Init(Scp079Role role, ReferenceHub owner)
	{
		base.Init(role, owner);
		role.SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._handler);
		role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		this._handler.OnStatusChanged += UpdateScreen;
		this._textFormat = Translations.Get(Scp079HudTranslation.ReconnectingEta);
	}

	private void OnDestroy()
	{
		this._handler.OnStatusChanged -= UpdateScreen;
	}

	private void UpdateScreen()
	{
		if (!this._handler.Lost)
		{
			this._rootObj.SetActive(value: false);
			return;
		}
		this._rootObj.SetActive(value: true);
		FacilityZone zone = this._curCamSync.CurrentCamera.Room.Zone;
		this.PlayStart(zone);
		this.PlayLoop(zone);
	}

	private void Update()
	{
		this._etaText.text = string.Format(this._textFormat, Mathf.CeilToInt(this._handler.RemainingTime));
	}

	private void PlayStart(FacilityZone zone)
	{
		ZoneClip[] zoneStarts = this._zoneStarts;
		for (int i = 0; i < zoneStarts.Length; i++)
		{
			ZoneClip zoneClip = zoneStarts[i];
			if (zoneClip.Zone == zone)
			{
				AudioSourcePoolManager.Play2D(zoneClip.Clip);
				return;
			}
		}
		AudioSourcePoolManager.Play2D(this._fallbackStart);
	}

	private void PlayLoop(FacilityZone zone)
	{
		this._loopSource.clip = this._fallbackLoop;
		ZoneClip[] zoneLoops = this._zoneLoops;
		for (int i = 0; i < zoneLoops.Length; i++)
		{
			ZoneClip zoneClip = zoneLoops[i];
			if (zoneClip.Zone == zone)
			{
				this._loopSource.clip = zoneClip.Clip;
				break;
			}
		}
		this._loopSource.Play();
	}
}
