using System;
using System.Linq;
using CursorManagement;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Strangle : KeySubroutine<Scp3114Role>, ICursorOverride
{
	private enum RpcType
	{
		TargetResync,
		TargetKilled,
		AttackInterrupted,
		OutOfRange
	}

	public readonly struct StrangleTarget
	{
		public readonly ReferenceHub Target;

		public readonly RelativePosition TargetPosition;

		public readonly RelativePosition AttackerPosition;

		public void WriteSelf(NetworkWriter writer)
		{
			writer.WriteReferenceHub(this.Target);
			writer.WriteRelativePosition(this.TargetPosition);
			writer.WriteRelativePosition(this.AttackerPosition);
		}

		public StrangleTarget(ReferenceHub target, Vector3 targetPosition, Vector3 attackerPosition)
		{
			this.Target = target;
			this.TargetPosition = new RelativePosition(targetPosition);
			this.AttackerPosition = new RelativePosition(attackerPosition);
		}

		public StrangleTarget(ReferenceHub target, RelativePosition targetPosition, RelativePosition attackerPosition)
		{
			this.Target = target;
			this.TargetPosition = targetPosition;
			this.AttackerPosition = attackerPosition;
		}

		public StrangleTarget(ReferenceHub target, NetworkReader reader)
		{
			this.Target = target;
			this.TargetPosition = reader.ReadRelativePosition();
			this.AttackerPosition = reader.ReadRelativePosition();
		}
	}

	private static readonly CachedLayerMask BlockerMask = new CachedLayerMask("Default", "Door", "Glass");

	[SerializeField]
	private float _targetAcquisitionMinDot;

	[SerializeField]
	private float _targetAcquisitionMaxDistance;

	[SerializeField]
	private float _stranglePositionOffset;

	[SerializeField]
	private float _strangleSqrCutoffHorizontal;

	[SerializeField]
	private float _strangleAbsCutoffVertical;

	[SerializeField]
	private float _onReleaseCooldown;

	[SerializeField]
	private float _onInterruptedCooldown;

	[SerializeField]
	private float _onKillCooldown;

	[SerializeField]
	private float _maxKeyHoldTime;

	[SerializeField]
	private float _attackerDamageImmunityTime;

	private Scp3114Slap _attackAbility;

	private ReferenceHub _clientDesiredTargetHub;

	private FpcStandardRoleBase _clientDesiredTargetRole;

	private Vector3 _validatedPosition;

	private bool _clientTargetting;

	private float _keyHoldingTime;

	private bool _warningHintAlreadyDisplayed;

	private RpcType _rpcType;

	public readonly AbilityCooldown ClientCooldown = new AbilityCooldown();

	public StrangleTarget? SyncTarget { get; private set; }

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement
	{
		get
		{
			if (this._clientTargetting)
			{
				return base.Role.IsLocalPlayer;
			}
			return false;
		}
	}

	protected override ActionName TargetKey => ActionName.Zoom;

	public event Action OnAttemptedWhileDisguised;

	public event Action ServerOnKill;

	public event Action ServerOnBegin;

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		if (this._clientTargetting)
		{
			Vector3 position = this._clientDesiredTargetRole.FpcModule.Position;
			writer.WriteReferenceHub(this._clientDesiredTargetHub);
			writer.WriteRelativePosition(new RelativePosition(position));
			Vector3 position2 = base.CastRole.FpcModule.Position;
			writer.WriteRelativePosition(new RelativePosition(position2));
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		StrangleTarget? syncTarget = this.ProcessAttackRequest(reader);
		bool hasValue = syncTarget.HasValue;
		if (hasValue != this.SyncTarget.HasValue && hasValue)
		{
			this.ServerOnBegin?.Invoke();
		}
		this.SyncTarget = syncTarget;
		this._rpcType = RpcType.TargetResync;
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._rpcType);
		this.SyncTarget?.WriteSelf(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		ReferenceHub hub = null;
		RpcType rpcType = (RpcType)reader.ReadByte();
		bool flag = reader.Remaining > 0 && reader.TryReadReferenceHub(out hub) && HitboxIdentity.IsEnemy(base.Owner, hub);
		this.SyncTarget = (flag ? new StrangleTarget?(new StrangleTarget(hub, reader)) : ((StrangleTarget?)null));
		if (flag)
		{
			if (base.Role.IsLocalPlayer)
			{
				base.CastRole.FpcModule.Position = this.SyncTarget.Value.AttackerPosition.Position;
			}
			return;
		}
		switch (rpcType)
		{
		case RpcType.AttackInterrupted:
			this.ClientCooldown.Trigger(this._onInterruptedCooldown);
			break;
		case RpcType.TargetKilled:
			this.ClientCooldown.Trigger(this._onKillCooldown);
			break;
		default:
			this.ClientCooldown.Trigger(this._onReleaseCooldown);
			break;
		}
		this._clientTargetting = false;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		CursorManager.Register(this);
		ReferenceHub.OnPlayerRemoved += OnPlayerRemoved;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
		base.Owner.playerStats.OnThisPlayerDamaged += OnThisPlayerDamaged;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		CursorManager.Unregister(this);
		this.SyncTarget = null;
		this.ClientCooldown.Clear();
		this._clientTargetting = false;
		ReferenceHub.OnPlayerRemoved -= OnPlayerRemoved;
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
		PlayerStats.OnAnyPlayerDied -= OnAnyPlayerDied;
		base.Owner.playerStats.OnThisPlayerDamaged -= OnThisPlayerDamaged;
	}

	protected override void OnKeyUp()
	{
		base.OnKeyUp();
		this._keyHoldingTime = 0f;
		this._warningHintAlreadyDisplayed = false;
		if (this._clientTargetting)
		{
			this._clientTargetting = false;
			this.ClientCooldown.Trigger(this._onReleaseCooldown);
			base.ClientSendCmd();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp3114Slap>(out this._attackAbility);
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active && this.SyncTarget.HasValue)
		{
			this.ServerUpdateTarget();
		}
		if (this._clientTargetting)
		{
			this.ClientUpdateTarget();
		}
		else if (this.IsKeyHeld && this.ClientCooldown.IsReady && this._attackAbility.Cooldown.IsReady && this._keyHoldingTime < this._maxKeyHoldTime)
		{
			this.ClientAttack();
			this._keyHoldingTime += Time.deltaTime;
		}
	}

	private void OnThisPlayerDamaged(DamageHandlerBase dhb)
	{
		if (this.SyncTarget.HasValue)
		{
			Strangled effect = this.SyncTarget.Value.Target.playerEffectsController.GetEffect<Strangled>();
			if (!effect.IsEnabled || !(effect.ElapsedSeconds < (double)this._attackerDamageImmunityTime))
			{
				this.SyncTarget = null;
				this._rpcType = RpcType.AttackInterrupted;
				base.ServerSendRpc(toAll: true);
			}
		}
	}

	private void ServerUpdateTarget()
	{
		PlayerRoleBase currentRole = this.SyncTarget.Value.Target.roleManager.CurrentRole;
		Vector3 v = base.CastRole.FpcModule.Position - (currentRole as IFpcRole).FpcModule.Position;
		float num = v.MagnitudeOnlyY();
		float num2 = v.SqrMagnitudeIgnoreY();
		if (num > this._strangleAbsCutoffVertical || num2 > this._strangleSqrCutoffHorizontal)
		{
			this.SyncTarget = null;
			this._rpcType = RpcType.OutOfRange;
			base.ServerSendRpc(toAll: true);
		}
	}

	private void OnAnyPlayerDied(ReferenceHub deadPly, DamageHandlerBase handler)
	{
		if (handler is Scp3114DamageHandler { Subtype: Scp3114DamageHandler.HandlerType.Strangulation } scp3114DamageHandler && !(scp3114DamageHandler.Attacker.Hub != base.Owner))
		{
			this.ServerOnKill?.Invoke();
		}
	}

	private void ClientUpdateTarget()
	{
		if (this._clientDesiredTargetRole == null || this._clientDesiredTargetRole.Pooled)
		{
			this._clientTargetting = false;
		}
		else if (!this._clientDesiredTargetRole.FpcModule.Motor.IsInvisible)
		{
			base.CastRole.LookAtPoint(this._clientDesiredTargetRole.CameraPosition);
		}
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		this.OnPlayerRemoved(userHub);
	}

	private void OnPlayerRemoved(ReferenceHub hub)
	{
		if (this.SyncTarget.HasValue && !(this.SyncTarget.Value.Target != hub))
		{
			this.SyncTarget = null;
			if (this._clientTargetting)
			{
				this._clientTargetting = false;
				this.ClientCooldown.Trigger(this._onKillCooldown);
			}
			if (NetworkServer.active)
			{
				this._rpcType = RpcType.TargetKilled;
				base.ServerSendRpc(toAll: true);
			}
		}
	}

	private void ClientAttack()
	{
		Transform playerCameraReference = base.Owner.PlayerCameraReference;
		ReferenceHub primaryTarget = ReferenceHub.AllHubs.Where(ValidateTarget).GetPrimaryTarget(playerCameraReference);
		if (primaryTarget == null || !(primaryTarget.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase) || !HitboxIdentity.IsEnemy(base.Owner, primaryTarget) || fpcStandardRoleBase.GetDot(base.Owner.PlayerCameraReference) < this._targetAcquisitionMinDot)
		{
			return;
		}
		switch (base.CastRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
			if (!this._warningHintAlreadyDisplayed)
			{
				this.OnAttemptedWhileDisguised?.Invoke();
			}
			this._warningHintAlreadyDisplayed = true;
			break;
		case Scp3114Identity.DisguiseStatus.Equipping:
			break;
		default:
			this._clientTargetting = true;
			this._clientDesiredTargetHub = primaryTarget;
			this._clientDesiredTargetRole = fpcStandardRoleBase;
			base.ClientSendCmd();
			break;
		}
	}

	private StrangleTarget? ProcessAttackRequest(NetworkReader reader)
	{
		if (reader.Remaining == 0)
		{
			return null;
		}
		if (this.SyncTarget.HasValue)
		{
			return null;
		}
		if (!reader.TryReadReferenceHub(out var hub))
		{
			return null;
		}
		Vector3 position = reader.ReadRelativePosition().Position;
		Vector3 position2 = reader.ReadRelativePosition().Position;
		StrangleTarget value;
		using (new FpcBacktracker(hub, position))
		{
			using (new FpcBacktracker(base.Owner, position2, Quaternion.identity))
			{
				if (!this.ValidateTarget(hub))
				{
					return null;
				}
				hub.playerEffectsController.EnableEffect<Strangled>();
				value = new StrangleTarget(hub, this.GetStranglePosition(hub.roleManager.CurrentRole as IFpcRole), base.CastRole.FpcModule.Position);
			}
		}
		base.CastRole.FpcModule.Position = this._validatedPosition;
		return value;
	}

	private Vector3 GetStranglePosition(IFpcRole targetFpc)
	{
		FirstPersonMovementModule fpcModule = base.CastRole.FpcModule;
		Vector3 normalized = (targetFpc.FpcModule.Position - fpcModule.Position).normalized;
		this._validatedPosition = fpcModule.Position;
		fpcModule.CharController.Move(normalized * this._stranglePositionOffset);
		return base.transform.position;
	}

	private bool ValidateTarget(ReferenceHub player)
	{
		if (!HitboxIdentity.IsEnemy(base.Owner, player))
		{
			return false;
		}
		if (!(player.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		Vector3 position = fpcRole.FpcModule.Position;
		Vector3 position2 = base.CastRole.FpcModule.Position;
		float targetAcquisitionMaxDistance = this._targetAcquisitionMaxDistance;
		float num = targetAcquisitionMaxDistance * targetAcquisitionMaxDistance;
		if ((position - position2).sqrMagnitude > num)
		{
			return false;
		}
		Vector3 position3 = player.PlayerCameraReference.position;
		return !Physics.Linecast(base.Owner.PlayerCameraReference.position, position3, Scp3114Strangle.BlockerMask);
	}
}
