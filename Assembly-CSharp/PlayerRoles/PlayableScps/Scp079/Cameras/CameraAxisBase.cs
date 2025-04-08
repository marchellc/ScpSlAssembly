using System;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public abstract class CameraAxisBase
	{
		private bool IsFirstperson
		{
			get
			{
				return Scp079Role.LocalInstanceActive;
			}
		}

		private bool IsSpectating
		{
			get
			{
				if (!SpectatorTargetTracker.TrackerSet)
				{
					return false;
				}
				Scp079SpectatableModule scp079SpectatableModule = SpectatorTargetTracker.CurrentTarget as Scp079SpectatableModule;
				return scp079SpectatableModule != null && scp079SpectatableModule != null && !scp079SpectatableModule.MainRole.Pooled;
			}
		}

		protected virtual float SpectatorLerpMultiplier
		{
			get
			{
				return 0f;
			}
		}

		public float CurValue { get; internal set; }

		public float MinValue
		{
			get
			{
				return this._constraints.x;
			}
		}

		public float MaxValue
		{
			get
			{
				return this._constraints.y;
			}
		}

		public float TargetValue
		{
			get
			{
				return this._val;
			}
			set
			{
				this._val = Mathf.Clamp(value % 360f, this._constraints.x, this._constraints.y);
				if (this._wasEverSet)
				{
					return;
				}
				this.CurValue = this._val;
				this._wasEverSet = true;
				this._wasMoving = true;
				this.OnValueChanged(this._val, null);
			}
		}

		public ushort Value16BitCompression
		{
			get
			{
				return (ushort)this.Compress(this._constraints, this.TargetValue, 65535);
			}
			set
			{
				this.TargetValue = this.Uncompress(this._constraints, (float)value, 65535);
			}
		}

		public byte Value8BitCompression
		{
			get
			{
				return (byte)this.Compress(this._constraints, this.TargetValue, 255);
			}
			set
			{
				this.TargetValue = this.Uncompress(this._constraints, (float)value, 255);
			}
		}

		internal virtual void Update(Scp079Camera cam)
		{
			if (this.CurValue != this.TargetValue)
			{
				float num = Mathf.Abs(this.CurValue - this.TargetValue);
				bool isFirstperson = this.IsFirstperson;
				bool isSpectating = this.IsSpectating;
				float num2 = this.SpeedCurve.Evaluate(num);
				float num3 = this.VolumeCurve.Evaluate(num);
				float num4 = this.PitchCurve.Evaluate(num);
				float num5 = ((this._soundLerpSpeed == 0f) ? 1f : (this._soundLerpSpeed * Time.deltaTime));
				if (isFirstperson || isSpectating)
				{
					num3 *= 0.4f;
					num4 *= 1f;
					num5 *= 1f;
				}
				if (!this._wasMoving)
				{
					this.SoundEffectSource.Play();
					this._wasMoving = true;
				}
				if (isFirstperson)
				{
					this.CurValue = Mathf.Clamp(this.CurValue, this.TargetValue - this._localPlayerDiffLimiter, this.TargetValue + this._localPlayerDiffLimiter);
				}
				else if (isSpectating)
				{
					this.CurValue = Mathf.Lerp(this.CurValue, this.TargetValue, Time.deltaTime * this.SpectatorLerpMultiplier);
				}
				num2 *= Time.deltaTime;
				this.CurValue = ((this._constraints.x == -360f || this._constraints.y == 360f) ? Mathf.MoveTowardsAngle(this.CurValue, this.TargetValue, num2) : Mathf.MoveTowards(this.CurValue, this.TargetValue, num2));
				this.SoundEffectSource.volume = Mathf.Lerp(this.SoundEffectSource.volume, num3, num5);
				this.SoundEffectSource.pitch = Mathf.Lerp(this.SoundEffectSource.pitch, num4, num5);
				this.OnValueChanged(this.CurValue, cam);
				return;
			}
			if (!this._wasMoving)
			{
				return;
			}
			float num6 = this.SoundEffectSource.volume - this._soundStopSpeed * Time.deltaTime;
			if (num6 <= 0f)
			{
				this.SoundEffectSource.Stop();
				this._wasMoving = false;
			}
			this.SoundEffectSource.volume = Mathf.Clamp01(num6);
		}

		internal virtual void Awake(Scp079Camera cam)
		{
			this._wasEverSet = false;
		}

		protected abstract void OnValueChanged(float newValue, Scp079Camera cam);

		private int Compress(Vector2 constraints, float val, int maxVal)
		{
			return Mathf.RoundToInt(Mathf.InverseLerp(constraints.x, constraints.y, val) * (float)maxVal);
		}

		private float Uncompress(Vector2 constraints, float val, int maxVal)
		{
			return Mathf.Lerp(constraints.x, constraints.y, val / (float)maxVal);
		}

		private float _val;

		private bool _wasEverSet;

		private bool _wasMoving;

		private const float LocalPlayerLerpMultiplier = 1f;

		private const float LocalPlayerPitchMultiplier = 1f;

		private const float LocalPlayerVolumeMultiplier = 0.4f;

		[SerializeField]
		private float _soundLerpSpeed;

		[SerializeField]
		private float _soundStopSpeed;

		[SerializeField]
		private float _localPlayerDiffLimiter;

		[SerializeField]
		private Vector2 _constraints;

		[SerializeField]
		protected AudioSource SoundEffectSource;

		[SerializeField]
		protected AnimationCurve SpeedCurve;

		[SerializeField]
		protected AnimationCurve VolumeCurve;

		[SerializeField]
		protected AnimationCurve PitchCurve;
	}
}
