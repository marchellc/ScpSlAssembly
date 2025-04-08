using System;
using System.Diagnostics;
using CameraShaking;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939FocusAbility : StandardSubroutine<Scp939Role>, IShakeEffect
	{
		public event Action OnStateChanged;

		public float State
		{
			get
			{
				return this._state;
			}
			set
			{
				value = Mathf.Clamp01(value);
				if (value == this._state)
				{
					return;
				}
				this._state = value;
				Action onStateChanged = this.OnStateChanged;
				if (onStateChanged == null)
				{
					return;
				}
				onStateChanged();
			}
		}

		public bool TargetState
		{
			get
			{
				return this._targetState || this._lungeAbility.State == Scp939LungeState.Triggered;
			}
			private set
			{
				if (this._targetState == value)
				{
					return;
				}
				if (value && this.State == 0f)
				{
					this._relativeWaypoint = this.CurrentWaypointId;
					this._relativeFreezeRot = WaypointBase.GetRelativeRotation(this._relativeWaypoint, this._ownerTransform.rotation).eulerAngles.y;
					this._frozenSw.Restart();
				}
				this._targetState = value;
				this._serverSendCooldown.Clear();
			}
		}

		public float FrozenTime
		{
			get
			{
				return (float)this._frozenSw.Elapsed.TotalSeconds;
			}
		}

		public float FrozenRotation
		{
			get
			{
				return WaypointBase.GetWorldRotation(this._relativeWaypoint, Quaternion.Euler(Vector3.up * this._relativeFreezeRot)).eulerAngles.y;
			}
		}

		public float AngularDeviation
		{
			get
			{
				if (this.State <= 0f)
				{
					return 0f;
				}
				return Mathf.DeltaAngle(this.FrozenRotation, this._ownerTransform.eulerAngles.y);
			}
		}

		private byte CurrentWaypointId
		{
			get
			{
				return new RelativePosition(base.CastRole.FpcModule.Position).WaypointId;
			}
		}

		private bool IsAvailable
		{
			get
			{
				return base.CastRole.FpcModule.IsGrounded;
			}
		}

		private void Update()
		{
			this.State += Time.deltaTime * (this.TargetState ? this._transitionSpeed : (-this._transitionSpeed));
			if (!this.TargetState)
			{
				this._focusInSource.volume -= Time.deltaTime * 10f;
			}
			if (!NetworkServer.active)
			{
				return;
			}
			this.TargetState = this.IsAvailable && this._keySync.FocusKeyHeld && this._clawAbility.Cooldown.IsReady && (this._targetState || this.State == 0f);
			this._muteEffect.IsEnabled = this.TargetState;
			this.UpdateRelativeRotation();
			if (!this._serverSendCooldown.IsReady)
			{
				return;
			}
			base.ServerSendRpc(true);
			this._serverSendCooldown.Trigger(1.5);
		}

		private void UpdateRelativeRotation()
		{
			if (!this.TargetState)
			{
				return;
			}
			byte currentWaypointId = this.CurrentWaypointId;
			if (currentWaypointId == this._relativeWaypoint)
			{
				return;
			}
			this._relativeFreezeRot = WaypointBase.GetRelativeRotation(currentWaypointId, Quaternion.Euler(Vector3.up * this.FrozenRotation)).eulerAngles.y;
			this._relativeWaypoint = currentWaypointId;
			this._serverSendCooldown.Clear();
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp939FocusKeySync>(out this._keySync);
			base.GetSubroutine<Scp939ClawAbility>(out this._clawAbility);
			base.GetSubroutine<Scp939LungeAbility>(out this._lungeAbility);
			this._lungeAbility.OnStateChanged += delegate(Scp939LungeState newState)
			{
				this._hitAnimationPlaying = newState == Scp939LungeState.LandHit;
				if (newState == Scp939LungeState.None)
				{
					return;
				}
				this.TargetState = false;
			};
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			int num = Mathf.RoundToInt(this._relativeFreezeRot * 64f) + 1;
			if (!this._targetState)
			{
				num *= -1;
			}
			writer.WriteShort((short)num);
			writer.WriteByte(this._relativeWaypoint);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			short num = reader.ReadShort();
			bool targetState = this._targetState;
			this._targetState = num > 0;
			this._relativeWaypoint = reader.ReadByte();
			this._relativeFreezeRot = ((float)Mathf.Abs((int)num) - 1f) / 64f;
			if (!this._targetState || targetState)
			{
				return;
			}
			if (this._focusInSource.isPlaying)
			{
				this._focusInSource.timeSamples = 0;
			}
			else
			{
				this._focusInSource.Play();
			}
			this._focusInSource.volume = 1f;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._ownerTransform = base.Owner.transform;
			this._offsetMultiplier = 1f;
			CameraShakeController.AddEffect(this);
			if (!NetworkServer.active)
			{
				return;
			}
			this._muteEffect = base.Owner.playerEffectsController.GetEffect<SoundtrackMute>();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._state = 0f;
			this._targetState = false;
		}

		public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
		{
			shakeValues = ShakeEffectValues.None;
			if (base.Role.Pooled)
			{
				return false;
			}
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return true;
			}
			if (this.State == 0f)
			{
				return true;
			}
			bool flag = base.Owner.isLocalPlayer && this._lungeAbility.State == Scp939LungeState.Triggered;
			this._offsetMultiplier = Mathf.Lerp(this._offsetMultiplier, (float)(flag ? 0 : 1), Time.deltaTime * 15f);
			float num = this._cameraHeightOffset.Evaluate(this.State) * this._offsetMultiplier;
			float num2 = this._cameraForwardOffset.Evaluate(this.State) * this._offsetMultiplier;
			Vector3 forward = this._ownerTransform.forward;
			RaycastHit raycastHit;
			float num3 = (Physics.SphereCast(ply.PlayerCameraReference.position + Vector3.up * num, 0.16f, forward, out raycastHit, 0.16f + num2, Scp939FocusAbility.VisibilityMask) ? Mathf.Max(0f, raycastHit.distance - 0.16f) : num2);
			Vector3 vector = Vector3.up * num + forward * num3;
			float num4 = Mathf.SmoothStep(1f, this._cameraFov + 1f, this.State);
			Vector3? vector2;
			float num5;
			if (!this._hitAnimationPlaying || !base.Owner.isLocalPlayer)
			{
				vector2 = new Vector3?(vector);
				num5 = num4;
				shakeValues = new ShakeEffectValues(null, null, vector2, num5, 0f, 0f);
				return true;
			}
			float num6 = -Time.deltaTime * 480f;
			if (this.State < 0.9f)
			{
				this._hitAnimationPlaying = false;
			}
			vector2 = new Vector3?(vector);
			num5 = num4;
			float num7 = num6;
			shakeValues = new ShakeEffectValues(null, null, vector2, num5, num7, 0f);
			return true;
		}

		private SoundtrackMute _muteEffect;

		private Scp939FocusKeySync _keySync;

		private Scp939ClawAbility _clawAbility;

		private Scp939LungeAbility _lungeAbility;

		private Transform _ownerTransform;

		private float _state;

		private bool _targetState;

		private float _offsetMultiplier;

		private float _relativeFreezeRot;

		private byte _relativeWaypoint;

		[SerializeField]
		private float _transitionSpeed;

		[SerializeField]
		private AudioSource _focusInSource;

		[SerializeField]
		private AnimationCurve _cameraHeightOffset;

		[SerializeField]
		private AnimationCurve _cameraForwardOffset;

		[SerializeField]
		private float _cameraFov;

		private bool _hitAnimationPlaying;

		private const int RotationSpeed = 480;

		private const float RotationRestartState = 0.9f;

		private const float OffsetLerpSpeed = 15f;

		private const float CamMinRadius = 0.16f;

		private const float SourceDecaySpeed = 10f;

		private static readonly CachedLayerMask VisibilityMask = new CachedLayerMask(new string[] { "Default", "Glass", "CCTV", "Door" });

		private readonly AbilityCooldown _serverSendCooldown = new AbilityCooldown();

		private readonly Stopwatch _frozenSw = new Stopwatch();

		private const int AngleSyncAccuracy = 64;

		private const float ResendCooldown = 1.5f;
	}
}
