using AudioPooling;
using Mirror;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace Audio;

public class DamagedAudio : MonoBehaviour
{
	private const double Cooldown = 0.25;

	private const float MinimumDamage = 2.5f;

	[SerializeField]
	private AudioClip[] _scpAudioClips;

	[SerializeField]
	private AudioClip[] _humanAudioClips;

	private double _lastTime;

	private ReferenceHub _owner;

	private void Start()
	{
		if (ReferenceHub.TryGetHub(base.gameObject, out this._owner))
		{
			this._owner.playerStats.GetModule<HealthStat>().OnStatChange += OnHealthStatChange;
			this._owner.playerStats.GetModule<AhpStat>().OnStatChange += OnHealthStatChange;
			this._owner.playerStats.GetModule<HumeShieldStat>().OnStatChange += OnHumeStatChange;
		}
	}

	private void OnDestroy()
	{
		this._owner.playerStats.GetModule<HealthStat>().OnStatChange -= OnHealthStatChange;
		this._owner.playerStats.GetModule<AhpStat>().OnStatChange -= OnHealthStatChange;
		this._owner.playerStats.GetModule<HumeShieldStat>().OnStatChange -= OnHumeStatChange;
	}

	private void OnHumeStatChange(float oldValue, float newValue)
	{
		this.PlayAudio(oldValue - newValue, isHume: true);
	}

	private void OnHealthStatChange(float oldValue, float newValue)
	{
		this.PlayAudio(oldValue - newValue, isHume: false);
	}

	private void PlayAudio(float damage, bool isHume)
	{
		if ((this._owner.isLocalPlayer || this._owner.IsLocallySpectated()) && !(damage <= 2.5f) && !(this._lastTime > NetworkTime.time))
		{
			this._lastTime = NetworkTime.time + 0.25;
			AudioSourcePoolManager.Play2DWithParent(isHume ? this._scpAudioClips.RandomItem() : this._humanAudioClips.RandomItem(), this._owner.transform);
		}
	}
}
