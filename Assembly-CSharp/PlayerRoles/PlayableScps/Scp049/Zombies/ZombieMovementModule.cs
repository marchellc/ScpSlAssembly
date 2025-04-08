using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieMovementModule : FirstPersonMovementModule
	{
		public bool CanMove { get; set; }

		public float BloodlustSpeed { get; private set; }

		public float NormalSpeed { get; private set; }

		private float MovementSpeed
		{
			get
			{
				return this.WalkSpeed;
			}
			set
			{
				this.WalkSpeed = value;
				this.SprintSpeed = value;
			}
		}

		public void ForceBloodlustSpeed()
		{
			this.MovementSpeed = this.BloodlustSpeed;
		}

		private void Awake()
		{
			this.NormalSpeed = this.WalkSpeed;
			this.BloodlustSpeed = this.SprintSpeed;
			this._role.SubroutineModule.TryGetSubroutine<ZombieBloodlustAbility>(out this._visionTracker);
		}

		protected override void UpdateMovement()
		{
			float deltaTime = Time.deltaTime;
			this.UpdateBloodlustState(deltaTime);
			this.UpdateSpeed(deltaTime);
			base.UpdateMovement();
		}

		private void UpdateBloodlustState(float deltaTime)
		{
			float num = this._lookingTimer + (this._visionTracker.LookingAtTarget ? deltaTime : (-deltaTime));
			this._lookingTimer = Mathf.Clamp(num, 0f, 5f);
			if (this._lookingTimer > 1f)
			{
				this._bloodlustActive = true;
				return;
			}
			if (this._lookingTimer == 0f)
			{
				this._bloodlustActive = false;
			}
		}

		private void UpdateSpeed(float deltaTime)
		{
			this._speedTickTimer += deltaTime;
			if (this._speedTickTimer < 1f)
			{
				return;
			}
			this._speedTickTimer = 0f;
			float num = this.MovementSpeed + (this._bloodlustActive ? 0.05f : (-0.1f));
			this.MovementSpeed = Mathf.Clamp(num, this.NormalSpeed, this.BloodlustSpeed);
		}

		public const float MaxTargetTime = 5f;

		private const float MinTargetTime = 1f;

		private const float SpeedPerTick = 0.05f;

		[SerializeField]
		private ZombieRole _role;

		private ZombieBloodlustAbility _visionTracker;

		private float _speedTickTimer;

		private bool _bloodlustActive;

		private float _lookingTimer;
	}
}
