using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp1507;
using UnityEngine;

namespace FacilitySoundtrack;

public class FlamingoSoundtrack : SoundtrackLayerBase
{
	[SerializeField]
	private float _fadeInWeightSpeed;

	[SerializeField]
	private float _fadeOutWeightSpeed;

	[SerializeField]
	private float _fadePresenceSpeed;

	[SerializeField]
	private AudioSource _ambient;

	[SerializeField]
	private AnimationCurve _volumeOverElapsed;

	[SerializeField]
	private float _minSustainTime;

	[SerializeField]
	private float _omnipresentDuration;

	[SerializeField]
	private float _flamingoRangeSqr;

	private float _weight;

	private float _elapsed;

	private float _separationTimer;

	private const float LateJoinSkipTime = 60f;

	private const float AwayVolume = 0.01f;

	public override float Weight => this._weight;

	public override bool Additive => false;

	private void Update()
	{
		this.FindFlamingos(out var anyFlamingosExist, out var anyFlamingosInRange);
		if (!anyFlamingosExist)
		{
			this._weight = Mathf.MoveTowards(this._weight, 0f, this._fadeOutWeightSpeed * Time.deltaTime);
			return;
		}
		if (anyFlamingosInRange || this._elapsed < this._omnipresentDuration)
		{
			this._separationTimer = 0f;
		}
		else
		{
			this._separationTimer += Time.deltaTime;
		}
		float target = ((this._separationTimer < this._minSustainTime) ? 1f : 0.01f);
		this._weight = Mathf.MoveTowards(this._weight, target, this._fadePresenceSpeed * Time.deltaTime);
	}

	private void FindFlamingos(out bool anyFlamingosExist, out bool anyFlamingosInRange)
	{
		anyFlamingosExist = false;
		anyFlamingosInRange = false;
		ReferenceHub hub;
		Vector3? vector = ((ReferenceHub.TryGetLocalHub(out hub) && hub.roleManager.CurrentRole is IFpcRole fpcRole) ? new Vector3?(fpcRole.FpcModule.Position) : ((Vector3?)null));
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is Scp1507Role { Team: Team.Flamingos } scp1507Role)
			{
				anyFlamingosExist = true;
				if (!vector.HasValue)
				{
					break;
				}
				if (!((scp1507Role.FpcModule.Position - vector.Value).sqrMagnitude > this._flamingoRangeSqr))
				{
					anyFlamingosInRange = true;
					break;
				}
			}
		}
	}

	public override void UpdateVolume(float volumeScale)
	{
		bool flag = volumeScale <= 0f;
		if (this._ambient.isPlaying)
		{
			if (flag)
			{
				this._ambient.Stop();
			}
			this._elapsed += Time.deltaTime;
		}
		else
		{
			if (flag)
			{
				return;
			}
			this._ambient.Play();
		}
		this._ambient.volume = volumeScale * this._volumeOverElapsed.Evaluate(this._elapsed);
	}
}
