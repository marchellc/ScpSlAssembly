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

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Strangle : KeySubroutine<Scp3114Role>, ICursorOverride
	{
		public event Action OnAttemptedWhileDisguised;

		public event Action ServerOnKill;

		public event Action ServerOnBegin;

		public Scp3114Strangle.StrangleTarget? SyncTarget { get; private set; }

		public CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		public bool LockMovement
		{
			get
			{
				return this._clientTargetting && base.Role.IsLocalPlayer;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Zoom;
			}
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			if (!this._clientTargetting)
			{
				return;
			}
			Vector3 position = this._clientDesiredTargetRole.FpcModule.Position;
			writer.WriteReferenceHub(this._clientDesiredTargetHub);
			writer.WriteRelativePosition(new RelativePosition(position));
			Vector3 position2 = base.CastRole.FpcModule.Position;
			writer.WriteRelativePosition(new RelativePosition(position2));
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			Scp3114Strangle.StrangleTarget? strangleTarget = this.ProcessAttackRequest(reader);
			bool flag = strangleTarget != null;
			if (flag != (this.SyncTarget != null) && flag)
			{
				Action serverOnBegin = this.ServerOnBegin;
				if (serverOnBegin != null)
				{
					serverOnBegin();
				}
			}
			this.SyncTarget = strangleTarget;
			this._rpcType = Scp3114Strangle.RpcType.TargetResync;
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._rpcType);
			if (this.SyncTarget == null)
			{
				return;
			}
			Scp3114Strangle.StrangleTarget? strangleTarget;
			strangleTarget.GetValueOrDefault().WriteSelf(writer);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			ReferenceHub referenceHub = null;
			Scp3114Strangle.RpcType rpcType = (Scp3114Strangle.RpcType)reader.ReadByte();
			bool flag = reader.Remaining > 0 && reader.TryReadReferenceHub(out referenceHub) && HitboxIdentity.IsEnemy(base.Owner, referenceHub);
			this.SyncTarget = (flag ? new Scp3114Strangle.StrangleTarget?(new Scp3114Strangle.StrangleTarget(referenceHub, reader)) : null);
			if (flag)
			{
				if (base.Role.IsLocalPlayer)
				{
					base.CastRole.FpcModule.Position = this.SyncTarget.Value.AttackerPosition.Position;
				}
				return;
			}
			if (rpcType != Scp3114Strangle.RpcType.TargetKilled)
			{
				if (rpcType == Scp3114Strangle.RpcType.AttackInterrupted)
				{
					this.ClientCooldown.Trigger((double)this._onInterruptedCooldown);
				}
				else
				{
					this.ClientCooldown.Trigger((double)this._onReleaseCooldown);
				}
			}
			else
			{
				this.ClientCooldown.Trigger((double)this._onKillCooldown);
			}
			this._clientTargetting = false;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			CursorManager.Register(this);
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerDied;
			base.Owner.playerStats.OnThisPlayerDamaged += this.OnThisPlayerDamaged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			CursorManager.Unregister(this);
			this.SyncTarget = null;
			this.ClientCooldown.Clear();
			this._clientTargetting = false;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			PlayerStats.OnAnyPlayerDied -= this.OnAnyPlayerDied;
			base.Owner.playerStats.OnThisPlayerDamaged -= this.OnThisPlayerDamaged;
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			this._keyHoldingTime = 0f;
			this._warningHintAlreadyDisplayed = false;
			if (!this._clientTargetting)
			{
				return;
			}
			this._clientTargetting = false;
			this.ClientCooldown.Trigger((double)this._onReleaseCooldown);
			base.ClientSendCmd();
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp3114Slap>(out this._attackAbility);
		}

		protected override void Update()
		{
			base.Update();
			if (NetworkServer.active && this.SyncTarget != null)
			{
				this.ServerUpdateTarget();
			}
			if (this._clientTargetting)
			{
				this.ClientUpdateTarget();
				return;
			}
			if (this.IsKeyHeld && this.ClientCooldown.IsReady && this._attackAbility.Cooldown.IsReady && this._keyHoldingTime < this._maxKeyHoldTime)
			{
				this.ClientAttack();
				this._keyHoldingTime += Time.deltaTime;
			}
		}

		private void OnThisPlayerDamaged(DamageHandlerBase dhb)
		{
			if (this.SyncTarget == null)
			{
				return;
			}
			Strangled effect = this.SyncTarget.Value.Target.playerEffectsController.GetEffect<Strangled>();
			if (effect.IsEnabled && effect.ElapsedSeconds < (double)this._attackerDamageImmunityTime)
			{
				return;
			}
			this.SyncTarget = null;
			this._rpcType = Scp3114Strangle.RpcType.AttackInterrupted;
			base.ServerSendRpc(true);
		}

		private void ServerUpdateTarget()
		{
			PlayerRoleBase currentRole = this.SyncTarget.Value.Target.roleManager.CurrentRole;
			Vector3 vector = base.CastRole.FpcModule.Position - (currentRole as IFpcRole).FpcModule.Position;
			float num = vector.MagnitudeOnlyY();
			float num2 = vector.SqrMagnitudeIgnoreY();
			if (num > this._strangleAbsCutoffVertical || num2 > this._strangleSqrCutoffHorizontal)
			{
				this.SyncTarget = null;
				this._rpcType = Scp3114Strangle.RpcType.OutOfRange;
				base.ServerSendRpc(true);
			}
		}

		private void OnAnyPlayerDied(ReferenceHub deadPly, DamageHandlerBase handler)
		{
			Scp3114DamageHandler scp3114DamageHandler = handler as Scp3114DamageHandler;
			if (scp3114DamageHandler == null)
			{
				return;
			}
			if (scp3114DamageHandler.Subtype != Scp3114DamageHandler.HandlerType.Strangulation)
			{
				return;
			}
			if (scp3114DamageHandler.Attacker.Hub != base.Owner)
			{
				return;
			}
			Action serverOnKill = this.ServerOnKill;
			if (serverOnKill == null)
			{
				return;
			}
			serverOnKill();
		}

		private void ClientUpdateTarget()
		{
			if (this._clientDesiredTargetRole == null || this._clientDesiredTargetRole.Pooled)
			{
				this._clientTargetting = false;
				return;
			}
			if (this._clientDesiredTargetRole.FpcModule.Motor.IsInvisible)
			{
				return;
			}
			base.CastRole.LookAtPoint(this._clientDesiredTargetRole.CameraPosition, 1f);
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			this.OnPlayerRemoved(userHub);
		}

		private void OnPlayerRemoved(ReferenceHub hub)
		{
			if (this.SyncTarget == null || this.SyncTarget.Value.Target != hub)
			{
				return;
			}
			this.SyncTarget = null;
			if (this._clientTargetting)
			{
				this._clientTargetting = false;
				this.ClientCooldown.Trigger((double)this._onKillCooldown);
			}
			if (NetworkServer.active)
			{
				this._rpcType = Scp3114Strangle.RpcType.TargetKilled;
				base.ServerSendRpc(true);
			}
		}

		private void ClientAttack()
		{
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			ReferenceHub primaryTarget = ReferenceHub.AllHubs.Where(new Func<ReferenceHub, bool>(this.ValidateTarget)).GetPrimaryTarget(playerCameraReference);
			if (!(primaryTarget == null))
			{
				FpcStandardRoleBase fpcStandardRoleBase = primaryTarget.roleManager.CurrentRole as FpcStandardRoleBase;
				if (fpcStandardRoleBase != null)
				{
					if (!HitboxIdentity.IsEnemy(base.Owner, primaryTarget))
					{
						return;
					}
					if (fpcStandardRoleBase.GetDot(0.5f) < this._targetAcquisitionMinDot)
					{
						return;
					}
					Scp3114Identity.DisguiseStatus status = base.CastRole.CurIdentity.Status;
					if (status == Scp3114Identity.DisguiseStatus.Equipping)
					{
						return;
					}
					if (status == Scp3114Identity.DisguiseStatus.Active)
					{
						if (!this._warningHintAlreadyDisplayed)
						{
							Action onAttemptedWhileDisguised = this.OnAttemptedWhileDisguised;
							if (onAttemptedWhileDisguised != null)
							{
								onAttemptedWhileDisguised();
							}
						}
						this._warningHintAlreadyDisplayed = true;
						return;
					}
					this._clientTargetting = true;
					this._clientDesiredTargetHub = primaryTarget;
					this._clientDesiredTargetRole = fpcStandardRoleBase;
					base.ClientSendCmd();
					return;
				}
			}
		}

		private Scp3114Strangle.StrangleTarget? ProcessAttackRequest(NetworkReader reader)
		{
			Scp3114Strangle.StrangleTarget? strangleTarget;
			if (reader.Remaining == 0)
			{
				strangleTarget = null;
				return strangleTarget;
			}
			strangleTarget = this.SyncTarget;
			if (strangleTarget != null)
			{
				strangleTarget = null;
				return strangleTarget;
			}
			ReferenceHub referenceHub;
			if (!reader.TryReadReferenceHub(out referenceHub))
			{
				strangleTarget = null;
				return strangleTarget;
			}
			Vector3 position = reader.ReadRelativePosition().Position;
			Vector3 position2 = reader.ReadRelativePosition().Position;
			Scp3114Strangle.StrangleTarget strangleTarget2;
			using (new FpcBacktracker(referenceHub, position, 0.4f))
			{
				using (new FpcBacktracker(base.Owner, position2, Quaternion.identity, 0.1f, 0.15f))
				{
					if (!this.ValidateTarget(referenceHub))
					{
						strangleTarget = null;
						return strangleTarget;
					}
					referenceHub.playerEffectsController.EnableEffect<Strangled>(0f, false);
					strangleTarget2 = new Scp3114Strangle.StrangleTarget(referenceHub, this.GetStranglePosition(referenceHub.roleManager.CurrentRole as IFpcRole), base.CastRole.FpcModule.Position);
				}
			}
			base.CastRole.FpcModule.Position = this._validatedPosition;
			return new Scp3114Strangle.StrangleTarget?(strangleTarget2);
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
			IFpcRole fpcRole = player.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
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

		private static readonly CachedLayerMask BlockerMask = new CachedLayerMask(new string[] { "Default", "Door", "Glass" });

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

		private Scp3114Strangle.RpcType _rpcType;

		public readonly AbilityCooldown ClientCooldown = new AbilityCooldown();

		private enum RpcType
		{
			TargetResync,
			TargetKilled,
			AttackInterrupted,
			OutOfRange
		}

		public readonly struct StrangleTarget
		{
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

			public readonly ReferenceHub Target;

			public readonly RelativePosition TargetPosition;

			public readonly RelativePosition AttackerPosition;
		}
	}
}
