using System;
using PlayerRoles;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class AloneSoundtrack : SoundtrackLayerBase
	{
		public override bool Additive
		{
			get
			{
				return false;
			}
		}

		public override float Weight
		{
			get
			{
				return this._weight;
			}
		}

		public override void UpdateVolume(float masterScale)
		{
			ReferenceHub referenceHub;
			if (ReferenceHub.TryGetLocalHub(out referenceHub) && (!referenceHub.IsHuman() || referenceHub.GetRoleId() == RoleTypeId.Tutorial))
			{
				this._weight = 0f;
				this.SetVolume(this._weight);
				return;
			}
			bool flag = false;
			bool flag2 = false;
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				if (!referenceHub2.isLocalPlayer && referenceHub2.IsAlive() && referenceHub2.GetRoleId() != RoleTypeId.Tutorial && VisionInformation.GetVisionInformation(referenceHub, referenceHub.PlayerCameraReference, referenceHub2.PlayerCameraReference.position, 0.25f, 60f, true, true, 0, true).IsLooking)
				{
					if (!referenceHub2.IsSCP(true))
					{
						flag = true;
						break;
					}
					flag2 = true;
				}
			}
			if (!flag && this._hasFoundSCP != flag2)
			{
				this._hasFoundSCP = flag2;
				if (this._oldAloneTime != 0f)
				{
					this._aloneTime = ((90f < this._aloneTime) ? 90f : this._oldAloneTime);
				}
				this._oldAloneTime = (flag2 ? this._aloneTime : 0f);
			}
			this._aloneTime = (flag ? 0f : (this._aloneTime + Time.deltaTime));
			float num = ((base.IsPovMuted || this._oldAloneTime != 0f || 120f > this._aloneTime) ? (-this._fadeOutSpeed) : this._fadeInSpeed);
			this._weight = Mathf.Clamp01(this._weight + num * Time.deltaTime);
			this.SetVolume(this._weight * masterScale * this._maxVolume);
		}

		private void SetVolume(float volume)
		{
			this._aloneSource.volume = volume;
		}

		private void Start()
		{
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		private void OnDestroy()
		{
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!userHub.isLocalPlayer || newRole.Team == Team.Dead)
			{
				return;
			}
			this._weight = 0f;
			this._aloneTime = 0f;
			this._oldAloneTime = 0f;
			this._hasFoundSCP = false;
		}

		private const float AloneTime = 120f;

		private const float SCPBufferTime = 30f;

		private const float MaxVisionDistance = 60f;

		private const float CharacterRadius = 0.25f;

		[SerializeField]
		private float _fadeInSpeed;

		[SerializeField]
		private float _fadeOutSpeed;

		[SerializeField]
		private float _maxVolume;

		[SerializeField]
		private AudioSource _aloneSource;

		private float _weight;

		private float _aloneTime;

		private float _oldAloneTime;

		private bool _hasFoundSCP;
	}
}
