using System;
using GameObjectPools;
using InventorySystem.Items;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106StalkAbility : Scp106VigorAbilityBase, IPoolResettable, IInteractionBlocker
	{
		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Run;
			}
		}

		public override bool ServerWantsSubmerged
		{
			get
			{
				return this.StalkActive;
			}
		}

		public BlockedInteraction BlockedInteractions
		{
			get
			{
				return BlockedInteraction.All;
			}
		}

		public bool CanBeCleared
		{
			get
			{
				return !this.StalkActive;
			}
		}

		public bool StalkActive { get; private set; }

		private void UpdateServerside()
		{
			if (this._valueDirty)
			{
				this._valueDirty = false;
				base.ServerSendRpc(true);
			}
			if (this._sinkhole.IsDuringAnimation)
			{
				return;
			}
			if (this.StalkActive)
			{
				float num = (base.CastRole.FpcModule.Motor.MovementDetected ? 0.09f : 0.06f);
				base.VigorAmount -= Time.deltaTime * num;
				if (base.VigorAmount == 0f && base.CastRole.FpcModule.IsGrounded)
				{
					this.ServerSetStalk(false);
				}
				return;
			}
			this.UpdateMovementState();
		}

		private void UpdateMovementState()
		{
			if (Vector3.Distance(this._lastPosition, base.Owner.transform.position) > 5f)
			{
				this._lastPosition = base.Owner.transform.position;
				this._movementTime = NetworkTime.time + 2.0;
				this._isMoving = true;
			}
			if (NetworkTime.time > this._movementTime)
			{
				this._isMoving = false;
			}
			if (this._isMoving)
			{
				base.VigorAmount += 0.03f * Time.deltaTime;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this._sinkhole = base.CastRole.Sinkhole;
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase old, PlayerRoleBase cur)
			{
				if (NetworkServer.active && cur is SpectatorRole)
				{
					base.ServerSendRpc(hub);
				}
			};
		}

		protected override void Update()
		{
			base.Update();
			if (NetworkServer.active)
			{
				this.UpdateServerside();
			}
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this._sinkhole.ReadonlyCooldown.IsReady)
			{
				Scp106Hud.PlayFlash(false);
			}
			base.ClientSendCmd();
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (this._sinkhole.IsDuringAnimation)
			{
				return;
			}
			if (!this._sinkhole.ReadonlyCooldown.IsReady)
			{
				return;
			}
			if (!base.CastRole.FpcModule.IsGrounded)
			{
				return;
			}
			if (this.StalkActive)
			{
				this.ServerSetStalk(false);
				return;
			}
			if (base.VigorAmount < 0.35f)
			{
				if (base.Role.IsLocalPlayer)
				{
					Scp106Hud.PlayFlash(true);
				}
				this._rpcType = Scp106StalkAbility.RpcType.NotEnoughVigor;
				base.ServerSendRpc(false);
				return;
			}
			this.ServerSetStalk(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._rpcType);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (NetworkServer.active)
			{
				return;
			}
			switch (reader.ReadByte())
			{
			case 0:
				this.StalkActive = true;
				return;
			case 1:
				this.StalkActive = false;
				return;
			case 2:
				Scp106Hud.PlayFlash(true);
				return;
			default:
				return;
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.StalkActive = false;
			this._isMoving = false;
		}

		private void ServerSetStalk(bool stalkActive)
		{
			if (this.StalkActive == stalkActive)
			{
				return;
			}
			Scp106ChangingStalkModeEventArgs scp106ChangingStalkModeEventArgs = new Scp106ChangingStalkModeEventArgs(base.Owner, stalkActive);
			Scp106Events.OnChangingStalkMode(scp106ChangingStalkModeEventArgs);
			if (!scp106ChangingStalkModeEventArgs.IsAllowed)
			{
				return;
			}
			this.StalkActive = stalkActive;
			Scp106Events.OnChangedStalkMode(new Scp106ChangedStalkModeEventArgs(base.Owner, stalkActive));
			base.Owner.interCoordinator.AddBlocker(this);
			this._rpcType = (stalkActive ? Scp106StalkAbility.RpcType.StalkActive : Scp106StalkAbility.RpcType.StalkInactive);
			base.ServerSendRpc(true);
		}

		private const float VigorRegeneration = 0.03f;

		private const float VigorStalkCostStationary = 0.06f;

		private const float VigorStalkCostMoving = 0.09f;

		private const float MinVigorToSubmerge = 0.35f;

		private const BlockedInteraction Interactions = BlockedInteraction.All;

		private const float MovementRange = 5f;

		private const double MovementTimer = 2.0;

		private bool _valueDirty;

		private Scp106SinkholeController _sinkhole;

		private double _movementTime;

		private Vector3 _lastPosition;

		private bool _isMoving;

		private Scp106StalkAbility.RpcType _rpcType;

		private enum RpcType
		{
			StalkActive,
			StalkInactive,
			NotEnoughVigor
		}
	}
}
