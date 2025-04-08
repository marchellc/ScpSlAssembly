using System;
using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939LungeAbility : KeySubroutine<Scp939Role>
	{
		public event Action<Scp939LungeState> OnStateChanged;

		public bool IsReady
		{
			get
			{
				return this._focus.State == 1f && this.State == Scp939LungeState.None && (!base.Owner.isLocalPlayer || !Cursor.visible) && Mathf.Abs(this._focus.AngularDeviation) < this._lungeAngleLimit;
			}
		}

		public bool LungeRequested { get; private set; }

		public RelativePosition TriggerPos { get; private set; }

		public float LungeForwardSpeed
		{
			get
			{
				return this._forwardSpeedOverPitch.Evaluate(this._lungePitch);
			}
		}

		public float LungeJumpSpeed
		{
			get
			{
				return this._jumpSpeedOverPitch.Evaluate(this._lungePitch);
			}
		}

		public RagdollAnimationTemplate LungeDeathAnim { get; private set; }

		public Scp939LungeState State
		{
			get
			{
				return this._state;
			}
			private set
			{
				if (this.State == value)
				{
					return;
				}
				Scp939LungingEventArgs scp939LungingEventArgs = new Scp939LungingEventArgs(base.Owner, value);
				Scp939Events.OnLunging(scp939LungingEventArgs);
				if (!scp939LungingEventArgs.IsAllowed || scp939LungingEventArgs.LungeState == this.State)
				{
					return;
				}
				value = scp939LungingEventArgs.LungeState;
				this._state = value;
				Action<Scp939LungeState> onStateChanged = this.OnStateChanged;
				if (onStateChanged != null)
				{
					onStateChanged(value);
				}
				if (!base.Owner.isLocalPlayer)
				{
					this._movementModule.MouseLook.UpdateRotation();
				}
				this._lungePitch = this._movementModule.MouseLook.CurrentVertical;
				base.ServerSendRpc(true);
				Scp939Events.OnLunged(new Scp939LungedEventArgs(base.Owner, value));
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		private bool HasAuthority
		{
			get
			{
				return NetworkServer.active || base.Owner.isLocalPlayer;
			}
		}

		private RelativePosition CurPos
		{
			get
			{
				return new RelativePosition(base.CastRole.FpcModule.Position);
			}
		}

		private void OnGrounded()
		{
			if (!this.HasAuthority || this.State != Scp939LungeState.Triggered)
			{
				return;
			}
			this.State = ((this.CurPos.Position.y < this.TriggerPos.Position.y - this._harshLandingHeight) ? Scp939LungeState.LandHarsh : Scp939LungeState.LandRegular);
		}

		private void OnFocusStateChanged()
		{
			if (this._focus.State > 0f)
			{
				return;
			}
			this.State = Scp939LungeState.None;
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			this.LungeRequested = true;
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp939FocusAbility>(out this._focus);
			this._movementModule = base.CastRole.FpcModule as Scp939MovementModule;
			this._focus.OnStateChanged += this.OnFocusStateChanged;
			this._audio.Init(this);
		}

		protected override void Update()
		{
			this.LungeRequested = false;
			base.Update();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			Scp939MovementModule movementModule = this._movementModule;
			movementModule.OnGrounded = (Action)Delegate.Combine(movementModule.OnGrounded, new Action(this.OnGrounded));
		}

		public override void ResetObject()
		{
			this.LungeRequested = false;
			this.State = Scp939LungeState.None;
			Scp939MovementModule movementModule = this._movementModule;
			movementModule.OnGrounded = (Action)Delegate.Remove(movementModule.OnGrounded, new Action(this.OnGrounded));
		}

		public void TriggerLunge()
		{
			this.TriggerPos = this.CurPos;
			this.State = Scp939LungeState.Triggered;
		}

		public void ClientSendHit(FpcStandardRoleBase targetRole)
		{
			this._playerToHit = targetRole;
			this.State = Scp939LungeState.LandHit;
			base.ClientSendCmd();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
			ReferenceHub referenceHub;
			writer.WriteReferenceHub(this._playerToHit.TryGetOwner(out referenceHub) ? referenceHub : null);
			writer.WriteRelativePosition(new RelativePosition(this._playerToHit.FpcModule.Position));
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			Vector3 vector = reader.ReadRelativePosition().Position;
			ReferenceHub referenceHub = reader.ReadReferenceHub();
			RelativePosition relativePosition = reader.ReadRelativePosition();
			if (this.State != Scp939LungeState.Triggered)
			{
				if (!this.IsReady)
				{
					return;
				}
				this.TriggerLunge();
			}
			if (referenceHub == null || !HitboxIdentity.IsEnemy(base.Owner, referenceHub))
			{
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = referenceHub.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return;
			}
			FirstPersonMovementModule fpcModule = fpcStandardRoleBase.FpcModule;
			using (new FpcBacktracker(referenceHub, relativePosition.Position, 0.4f))
			{
				using (new FpcBacktracker(base.Owner, fpcModule.Position, Quaternion.identity, 0.1f, 0.15f))
				{
					Vector3 vector2 = fpcModule.Position - base.CastRole.FpcModule.Position;
					if (vector2.SqrMagnitudeIgnoreY() > this._overallTolerance * this._overallTolerance)
					{
						return;
					}
					if (vector2.y > this._overallTolerance || vector2.y < -this._bottomTolerance)
					{
						return;
					}
				}
			}
			using (new FpcBacktracker(base.Owner, vector, Quaternion.identity, 0.1f, 0.15f))
			{
				vector = base.CastRole.FpcModule.Position;
			}
			Transform transform = referenceHub.transform;
			Vector3 position = fpcModule.Position;
			Quaternion rotation = transform.rotation;
			Vector3 vector3 = new Vector3(vector.x, position.y, vector.z);
			transform.forward = -base.Owner.transform.forward;
			fpcModule.Position = vector3;
			bool flag = false;
			if (!Physics.Linecast(vector, position, PlayerRolesUtils.BlockerMask))
			{
				float num = 120f;
				Scp939AttackingEventArgs scp939AttackingEventArgs = new Scp939AttackingEventArgs(base.Owner, referenceHub, num);
				Scp939Events.OnAttacking(scp939AttackingEventArgs);
				if (!scp939AttackingEventArgs.IsAllowed)
				{
					return;
				}
				referenceHub = scp939AttackingEventArgs.Target.ReferenceHub;
				num = scp939AttackingEventArgs.Damage;
				flag = referenceHub.playerStats.DealDamage(new Scp939DamageHandler(base.CastRole, num, Scp939DamageType.LungeTarget));
				Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, referenceHub, num));
			}
			float num2 = (flag ? 1f : 0f);
			if (!flag || referenceHub.IsAlive())
			{
				fpcModule.Position = position;
				transform.rotation = rotation;
			}
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				if (!(referenceHub2 == referenceHub) && HitboxIdentity.IsEnemy(base.Owner, referenceHub2))
				{
					FpcStandardRoleBase fpcStandardRoleBase2 = referenceHub2.roleManager.CurrentRole as FpcStandardRoleBase;
					if (fpcStandardRoleBase2 != null)
					{
						Vector3 position2 = fpcStandardRoleBase2.FpcModule.Position;
						if ((fpcStandardRoleBase2.FpcModule.Position - vector3).sqrMagnitude <= this._secondaryRangeSqr)
						{
							if (Physics.Linecast(position2, vector, PlayerRolesUtils.BlockerMask))
							{
								return;
							}
							float num3 = 30f;
							Scp939AttackingEventArgs scp939AttackingEventArgs2 = new Scp939AttackingEventArgs(base.Owner, referenceHub, num3);
							Scp939Events.OnAttacking(scp939AttackingEventArgs2);
							if (!scp939AttackingEventArgs2.IsAllowed)
							{
								return;
							}
							referenceHub = scp939AttackingEventArgs2.Target.ReferenceHub;
							num3 = scp939AttackingEventArgs2.Damage;
							if (referenceHub2.playerStats.DealDamage(new Scp939DamageHandler(base.CastRole, num3, Scp939DamageType.LungeSecondary)))
							{
								Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, referenceHub, num3));
								flag = true;
								num2 = Mathf.Max(num2, 0.6f);
							}
						}
					}
				}
			}
			if (flag)
			{
				Hitmarker.SendHitmarkerDirectly(base.Owner, num2, true);
			}
			this.State = Scp939LungeState.LandHit;
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this.State);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (this.HasAuthority)
			{
				return;
			}
			this.State = (Scp939LungeState)reader.ReadByte();
		}

		[SerializeField]
		private float _harshLandingHeight;

		[SerializeField]
		private float _lungeAngleLimit;

		[SerializeField]
		private float _overallTolerance;

		[SerializeField]
		private float _bottomTolerance;

		[SerializeField]
		private float _secondaryRangeSqr;

		[SerializeField]
		private Scp939LungeAudio _audio;

		[SerializeField]
		private AnimationCurve _jumpSpeedOverPitch;

		[SerializeField]
		private AnimationCurve _forwardSpeedOverPitch;

		private Scp939FocusAbility _focus;

		private Scp939MovementModule _movementModule;

		private FpcStandardRoleBase _playerToHit;

		private Scp939LungeState _state;

		private float _lungePitch;

		private const float MainHitmarkerSize = 1f;

		private const float SecondaryHitmarkerSize = 0.6f;

		public const float LungeDamage = 120f;

		public const float SecondaryDamage = 30f;
	}
}
