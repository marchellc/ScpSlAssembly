using System;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp1507;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class FlamingoSoundtrack : SoundtrackLayerBase
	{
		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public override bool Additive
		{
			get
			{
				return false;
			}
		}

		private void Update()
		{
			bool flag;
			bool flag2;
			this.FindFlamingos(out flag, out flag2);
			if (!flag)
			{
				this._weight = Mathf.MoveTowards(this._weight, 0f, this._fadeOutWeightSpeed * Time.deltaTime);
				return;
			}
			if (flag2 || this._elapsed < this._omnipresentDuration)
			{
				this._separationTimer = 0f;
			}
			else
			{
				this._separationTimer += Time.deltaTime;
			}
			float num = ((this._separationTimer < this._minSustainTime) ? 1f : 0.01f);
			this._weight = Mathf.MoveTowards(this._weight, num, this._fadePresenceSpeed * Time.deltaTime);
		}

		private void FindFlamingos(out bool anyFlamingosExist, out bool anyFlamingosInRange)
		{
			anyFlamingosExist = false;
			anyFlamingosInRange = false;
			ReferenceHub referenceHub;
			Vector3? vector;
			if (ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					vector = new Vector3?(fpcRole.FpcModule.Position);
					goto IL_003E;
				}
			}
			vector = null;
			IL_003E:
			Vector3? vector2 = vector;
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				Scp1507Role scp1507Role = referenceHub2.roleManager.CurrentRole as Scp1507Role;
				if (scp1507Role != null && scp1507Role.Team == Team.Flamingos)
				{
					anyFlamingosExist = true;
					if (vector2 == null)
					{
						break;
					}
					if ((scp1507Role.FpcModule.Position - vector2.Value).sqrMagnitude <= this._flamingoRangeSqr)
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
	}
}
