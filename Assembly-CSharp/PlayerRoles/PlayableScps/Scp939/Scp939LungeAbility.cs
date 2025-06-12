using System;
using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939LungeAbility : KeySubroutine<Scp939Role>
{
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

	public bool IsReady
	{
		get
		{
			if (this._focus.State == 1f && this.State == Scp939LungeState.None && (!base.Owner.isLocalPlayer || !Cursor.visible))
			{
				return Mathf.Abs(this._focus.AngularDeviation) < this._lungeAngleLimit;
			}
			return false;
		}
	}

	public bool LungeRequested { get; private set; }

	public RelativePosition TriggerPos { get; private set; }

	public float LungeForwardSpeed => this._forwardSpeedOverPitch.Evaluate(this._lungePitch);

	public float LungeJumpSpeed => this._jumpSpeedOverPitch.Evaluate(this._lungePitch);

	[field: SerializeField]
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
			Scp939LungingEventArgs e = new Scp939LungingEventArgs(base.Owner, value);
			Scp939Events.OnLunging(e);
			if (e.IsAllowed && e.LungeState != this.State)
			{
				value = e.LungeState;
				this._state = value;
				this.OnStateChanged?.Invoke(value);
				if (!base.Owner.isLocalPlayer)
				{
					this._movementModule.MouseLook.UpdateRotation();
				}
				this._lungePitch = this._movementModule.MouseLook.CurrentVertical;
				base.ServerSendRpc(toAll: true);
				Scp939Events.OnLunged(new Scp939LungedEventArgs(base.Owner, value));
			}
		}
	}

	protected override ActionName TargetKey => ActionName.Shoot;

	private bool HasAuthority
	{
		get
		{
			if (!NetworkServer.active)
			{
				return base.Owner.isLocalPlayer;
			}
			return true;
		}
	}

	private RelativePosition CurPos => new RelativePosition(base.CastRole.FpcModule.Position);

	public event Action<Scp939LungeState> OnStateChanged;

	private void OnGrounded()
	{
		if (this.HasAuthority && this.State == Scp939LungeState.Triggered)
		{
			bool flag = this.CurPos.Position.y < this.TriggerPos.Position.y - this._harshLandingHeight;
			this.State = (flag ? Scp939LungeState.LandHarsh : Scp939LungeState.LandRegular);
		}
	}

	private void OnFocusStateChanged()
	{
		if (!(this._focus.State > 0f))
		{
			this.State = Scp939LungeState.None;
		}
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
		this._focus.OnStateChanged += OnFocusStateChanged;
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
		movementModule.OnGrounded = (Action)Delegate.Combine(movementModule.OnGrounded, new Action(OnGrounded));
	}

	public override void ResetObject()
	{
		this.LungeRequested = false;
		this.State = Scp939LungeState.None;
		Scp939MovementModule movementModule = this._movementModule;
		movementModule.OnGrounded = (Action)Delegate.Remove(movementModule.OnGrounded, new Action(OnGrounded));
	}

	public void TriggerLunge()
	{
		this.TriggerPos = this.CurPos;
		this.State = Scp939LungeState.Triggered;
	}

	public void ClientSendHit(FpcStandardRoleBase targetRole)
	{
		this._playerToHit = targetRole;
		base.ClientSendCmd();
		this.State = Scp939LungeState.LandHit;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
		writer.WriteReferenceHub(this._playerToHit.TryGetOwner(out var hub) ? hub : null);
		writer.WriteRelativePosition(new RelativePosition(this._playerToHit.FpcModule.Position));
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		Vector3 position = reader.ReadRelativePosition().Position;
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
		if (referenceHub == null || !HitboxIdentity.IsEnemy(base.Owner, referenceHub) || !(referenceHub.roleManager.CurrentRole is FpcStandardRoleBase { FpcModule: var fpcModule }))
		{
			return;
		}
		using (new FpcBacktracker(referenceHub, relativePosition.Position))
		{
			using (new FpcBacktracker(base.Owner, fpcModule.Position, Quaternion.identity))
			{
				Vector3 v = fpcModule.Position - base.CastRole.FpcModule.Position;
				if (v.SqrMagnitudeIgnoreY() > this._overallTolerance * this._overallTolerance || v.y > this._overallTolerance || v.y < 0f - this._bottomTolerance)
				{
					return;
				}
			}
		}
		using (new FpcBacktracker(base.Owner, position, Quaternion.identity))
		{
			position = base.CastRole.FpcModule.Position;
		}
		Transform transform = referenceHub.transform;
		Vector3 position2 = fpcModule.Position;
		Quaternion rotation = transform.rotation;
		Vector3 vector = new Vector3(position.x, position2.y, position.z);
		transform.forward = -base.Owner.transform.forward;
		fpcModule.Position = vector;
		bool flag = false;
		if (!Physics.Linecast(position, position2, PlayerRolesUtils.AttackMask))
		{
			float damage = 120f;
			Scp939AttackingEventArgs e = new Scp939AttackingEventArgs(base.Owner, referenceHub, damage);
			Scp939Events.OnAttacking(e);
			if (!e.IsAllowed)
			{
				return;
			}
			referenceHub = e.Target.ReferenceHub;
			damage = e.Damage;
			flag = referenceHub.playerStats.DealDamage(new Scp939DamageHandler(base.CastRole, damage, Scp939DamageType.LungeTarget));
			Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, referenceHub, damage));
		}
		float num = (flag ? 1f : 0f);
		if (!flag || referenceHub.IsAlive())
		{
			fpcModule.Position = position2;
			transform.rotation = rotation;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub == referenceHub || !HitboxIdentity.IsEnemy(base.Owner, allHub) || !(allHub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase2))
			{
				continue;
			}
			Vector3 position3 = fpcStandardRoleBase2.FpcModule.Position;
			if (!((fpcStandardRoleBase2.FpcModule.Position - vector).sqrMagnitude > this._secondaryRangeSqr))
			{
				if (Physics.Linecast(position3, position, PlayerRolesUtils.AttackMask))
				{
					return;
				}
				float damage2 = 30f;
				Scp939AttackingEventArgs e2 = new Scp939AttackingEventArgs(base.Owner, referenceHub, damage2);
				Scp939Events.OnAttacking(e2);
				if (!e2.IsAllowed)
				{
					return;
				}
				referenceHub = e2.Target.ReferenceHub;
				damage2 = e2.Damage;
				if (allHub.playerStats.DealDamage(new Scp939DamageHandler(base.CastRole, damage2, Scp939DamageType.LungeSecondary)))
				{
					Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, referenceHub, damage2));
					flag = true;
					num = Mathf.Max(num, 0.6f);
				}
			}
		}
		if (flag)
		{
			Hitmarker.SendHitmarkerDirectly(base.Owner, num);
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
		if (!this.HasAuthority)
		{
			this.State = (Scp939LungeState)reader.ReadByte();
		}
	}
}
