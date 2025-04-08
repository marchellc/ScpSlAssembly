using System;
using AudioPooling;
using Mirror;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace Audio
{
	public class DamagedAudio : MonoBehaviour
	{
		private void Start()
		{
			if (!ReferenceHub.TryGetHub(base.gameObject, out this._owner))
			{
				return;
			}
			this._owner.playerStats.GetModule<HealthStat>().OnStatChange += this.OnHealthStatChange;
			this._owner.playerStats.GetModule<AhpStat>().OnStatChange += this.OnHealthStatChange;
			this._owner.playerStats.GetModule<HumeShieldStat>().OnStatChange += this.OnHumeStatChange;
		}

		private void OnDestroy()
		{
			this._owner.playerStats.GetModule<HealthStat>().OnStatChange -= this.OnHealthStatChange;
			this._owner.playerStats.GetModule<AhpStat>().OnStatChange -= this.OnHealthStatChange;
			this._owner.playerStats.GetModule<HumeShieldStat>().OnStatChange -= this.OnHumeStatChange;
		}

		private void OnHumeStatChange(float oldValue, float newValue)
		{
			this.PlayAudio(oldValue - newValue, true);
		}

		private void OnHealthStatChange(float oldValue, float newValue)
		{
			this.PlayAudio(oldValue - newValue, false);
		}

		private void PlayAudio(float damage, bool isHume)
		{
			if (!this._owner.isLocalPlayer && !this._owner.IsLocallySpectated())
			{
				return;
			}
			if (damage <= 2.5f || this._lastTime > NetworkTime.time)
			{
				return;
			}
			this._lastTime = NetworkTime.time + 0.25;
			AudioSourcePoolManager.Play2DWithParent(isHume ? this._scpAudioClips.RandomItem<AudioClip>() : this._humanAudioClips.RandomItem<AudioClip>(), this._owner.transform, 1f, MixerChannel.DefaultSfx, 1f);
		}

		private const double Cooldown = 0.25;

		private const float MinimumDamage = 2.5f;

		[SerializeField]
		private AudioClip[] _scpAudioClips;

		[SerializeField]
		private AudioClip[] _humanAudioClips;

		private double _lastTime;

		private ReferenceHub _owner;
	}
}
