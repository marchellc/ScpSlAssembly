using System;
using Interactables.Interobjects;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class RippleInstance : MonoBehaviour
	{
		public ParticleSystem MainParticleSystem { get; private set; }

		public bool InUse
		{
			get
			{
				return Time.timeSinceLevelLoad - this._setTime <= 0f;
			}
		}

		public void Set(Vector3 pos, Color col)
		{
			if (this._inElevator)
			{
				this._t.SetParent(null);
				this._inElevator = false;
			}
			this._pos = pos;
			this._t.position = pos;
			this._setTime = Time.timeSinceLevelLoad + 3f;
			this.MainParticleSystem.main.startColor = col;
			this.MainParticleSystem.Play(true);
		}

		private void OnDisable()
		{
			this._setTime = 0f;
		}

		private void OnDestroy()
		{
			ElevatorChamber.OnElevatorMoved -= this.OnElevatorMoved;
		}

		private void Awake()
		{
			this._t = base.transform;
			ElevatorChamber.OnElevatorMoved += this.OnElevatorMoved;
		}

		private void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
		{
			if (this._inElevator || !elevatorBounds.Contains(this._pos))
			{
				return;
			}
			this._t.SetParent(chamber.transform);
			this._t.position += deltaPos;
			this._inElevator = true;
		}

		private bool _inElevator;

		private Vector3 _pos;

		private Transform _t;

		private float _setTime;

		private const float MinDuration = 3f;
	}
}
