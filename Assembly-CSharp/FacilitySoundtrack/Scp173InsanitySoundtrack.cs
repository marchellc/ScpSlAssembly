using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp173;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class Scp173InsanitySoundtrack : SoundtrackLayerBase
	{
		public static event Scp173InsanitySoundtrack.PlayerEscapeScp173 OnPlayerEscapeScp173;

		private float CurTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		private bool IsObserved
		{
			get
			{
				return this._observed173sCount > 0;
			}
		}

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

		private void Awake()
		{
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			Scp173CharacterModel.OnFrozen += this.OnFrozen;
			this._shouldRemove = (Scp173Role x) => !this.IsObservedBy(x, this._localHub);
			this._ambientsCount = this._distanceAmbients.Length;
			this._volumeCurves = new AnimationCurve[this._ambientsCount];
			for (int i = 0; i < this._ambientsCount; i++)
			{
				this._volumeCurves[i] = this._distanceAmbients[i].GetCustomCurve(AudioSourceCurveType.CustomRolloff);
			}
		}

		private void OnDestroy()
		{
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			Scp173CharacterModel.OnFrozen -= this.OnFrozen;
		}

		private void Update()
		{
			ReferenceHub referenceHub;
			if (!this._isActive || !ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			if (this._observed173sCount > 0)
			{
				this._localHub = referenceHub;
				this._observed173sCount -= this._observed173s.RemoveWhere(this._shouldRemove);
				if (!this.IsObserved)
				{
					this._stopAmbientTime = this.CurTime + this._sustainTime;
				}
			}
			bool flag = this.IsObserved || this._stopAmbientTime > this.CurTime;
			bool flag2 = flag != this._prevPlay;
			this._weight = Mathf.Lerp(this._weight, (float)(flag ? 1 : 0), (flag ? this._fadeInLerp : this._fadeOutLerp) * Time.deltaTime);
			this._prevPlay = flag;
			if (flag2 && !flag)
			{
				this._encounterSource.PlayOneShot(this._goneClip);
				this._closeEncounter.ResetCooldown();
				this._farEncounter.ResetCooldown();
				Scp173InsanitySoundtrack.PlayerEscapeScp173 onPlayerEscapeScp = Scp173InsanitySoundtrack.OnPlayerEscapeScp173;
				if (onPlayerEscapeScp != null)
				{
					onPlayerEscapeScp(referenceHub);
				}
			}
			if (flag)
			{
				float num = this._distanceCap;
				foreach (Scp173Role scp173Role in this._observed173s)
				{
					float num2 = this.DistanceTo(scp173Role);
					if (num2 < num)
					{
						num = num2;
					}
				}
				this._lastDistance = Mathf.Lerp(this._lastDistance, num, Time.deltaTime * this._distanceLerp);
			}
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!userHub.isLocalPlayer)
			{
				return;
			}
			this._isActive = userHub.IsHuman();
			this._prevPlay = false;
			this._weight = 0f;
			this._observed173sCount = 0;
			this._stopAmbientTime = 0f;
		}

		private void OnFrozen(Scp173Role target)
		{
			if (this._observed173s.Add(target))
			{
				this._observed173sCount++;
			}
			if (this.DistanceTo(target) < this._closeEncounterDistanceThreshold && this._closeEncounter.TryUse(this.CurTime))
			{
				this._encounterSource.PlayOneShot(this._closeEncounter.Clip);
				return;
			}
			if (this._farEncounter.TryUse(this.CurTime))
			{
				this._encounterSource.PlayOneShot(this._farEncounter.Clip);
			}
		}

		private float DistanceTo(Scp173Role role)
		{
			return Vector3.Distance(role.FpcModule.Position, MainCameraController.CurrentCamera.position);
		}

		private bool IsObservedBy(Scp173Role scp173, ReferenceHub lhub)
		{
			Scp173ObserversTracker scp173ObserversTracker;
			return scp173.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out scp173ObserversTracker) && scp173ObserversTracker.IsObservedBy(lhub, 1f);
		}

		public override void UpdateVolume(float volumeScale)
		{
			for (int i = 0; i < this._ambientsCount; i++)
			{
				float num = volumeScale;
				if (num > 0f)
				{
					float num2 = this._lastDistance / this._distanceAmbients[i].maxDistance;
					num *= this._volumeCurves[i].Evaluate(num2);
				}
				this._distanceAmbients[i].volume = num;
				this._distanceAmbients[i].panStereo = this._lastScreenPosition;
			}
		}

		public const float PanLimit = 0.65f;

		[SerializeField]
		private float _fadeInLerp;

		[SerializeField]
		private float _fadeOutLerp;

		[SerializeField]
		private float _sustainTime;

		[SerializeField]
		private AudioSource[] _distanceAmbients;

		[SerializeField]
		private AudioSource _encounterSource;

		[SerializeField]
		private AudioClip _goneClip;

		[SerializeField]
		private Scp173InsanitySoundtrack.EncounterTrack _closeEncounter;

		[SerializeField]
		private Scp173InsanitySoundtrack.EncounterTrack _farEncounter;

		[SerializeField]
		private float _closeEncounterDistanceThreshold;

		[SerializeField]
		private float _distanceLerp;

		[SerializeField]
		private float _distanceCap;

		private ReferenceHub _localHub;

		private Predicate<Scp173Role> _shouldRemove;

		private readonly HashSet<Scp173Role> _observed173s = new HashSet<Scp173Role>();

		private AnimationCurve[] _volumeCurves;

		private int _observed173sCount;

		private int _ambientsCount;

		private bool _isActive;

		private bool _prevPlay;

		private float _stopAmbientTime;

		private float _weight;

		private float _lastDistance;

		private bool _cameraSet;

		private Camera _camera;

		private float _lastScreenPosition;

		public delegate void PlayerEscapeScp173(ReferenceHub hub);

		[Serializable]
		private class EncounterTrack
		{
			public bool TryUse(float currentTime)
			{
				if (currentTime < this._nextUse)
				{
					return false;
				}
				this._nextUse = currentTime + this.Cooldown;
				return true;
			}

			public void ResetCooldown()
			{
				this._nextUse = 0f;
			}

			private float _nextUse;

			public AudioClip Clip;

			public float Cooldown;
		}
	}
}
