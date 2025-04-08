using System;
using AudioPooling;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079LostSignalGui : Scp079GuiElementBase
	{
		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			role.SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._handler);
			role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
			this._handler.OnStatusChanged += this.UpdateScreen;
			this._textFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ReconnectingEta);
		}

		private void OnDestroy()
		{
			this._handler.OnStatusChanged -= this.UpdateScreen;
		}

		private void UpdateScreen()
		{
			if (!this._handler.Lost)
			{
				this._rootObj.SetActive(false);
				return;
			}
			this._rootObj.SetActive(true);
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
			foreach (Scp079LostSignalGui.ZoneClip zoneClip in this._zoneStarts)
			{
				if (zoneClip.Zone == zone)
				{
					AudioSourcePoolManager.Play2D(zoneClip.Clip, 1f, MixerChannel.DefaultSfx, 1f);
					return;
				}
			}
			AudioSourcePoolManager.Play2D(this._fallbackStart, 1f, MixerChannel.DefaultSfx, 1f);
		}

		private void PlayLoop(FacilityZone zone)
		{
			this._loopSource.clip = this._fallbackLoop;
			foreach (Scp079LostSignalGui.ZoneClip zoneClip in this._zoneLoops)
			{
				if (zoneClip.Zone == zone)
				{
					this._loopSource.clip = zoneClip.Clip;
					break;
				}
			}
			this._loopSource.Play();
		}

		[SerializeField]
		private GameObject _rootObj;

		[SerializeField]
		private AudioSource _loopSource;

		[SerializeField]
		private TextMeshProUGUI _etaText;

		[SerializeField]
		private Scp079LostSignalGui.ZoneClip[] _zoneStarts;

		[SerializeField]
		private AudioClip _fallbackStart;

		[SerializeField]
		private Scp079LostSignalGui.ZoneClip[] _zoneLoops;

		[SerializeField]
		private AudioClip _fallbackLoop;

		private Scp079LostSignalHandler _handler;

		private Scp079CurrentCameraSync _curCamSync;

		private string _textFormat;

		[Serializable]
		private struct ZoneClip
		{
			public FacilityZone Zone;

			public AudioClip Clip;
		}
	}
}
