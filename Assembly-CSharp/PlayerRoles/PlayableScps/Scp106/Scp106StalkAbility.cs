using GameObjectPools;
using InventorySystem.Items;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106StalkAbility : Scp106VigorAbilityBase, IPoolResettable, IInteractionBlocker
{
	private enum RpcType
	{
		StalkActive,
		StalkInactive,
		NotEnoughVigor
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

	private RpcType _rpcType;

	protected override ActionName TargetKey => ActionName.Run;

	public override bool ServerWantsSubmerged => this.StalkActive;

	public BlockedInteraction BlockedInteractions => BlockedInteraction.All;

	public bool CanBeCleared => !this.StalkActive;

	public bool StalkActive { get; private set; }

	private void UpdateServerside()
	{
		if (this._valueDirty)
		{
			this._valueDirty = false;
			base.ServerSendRpc(toAll: true);
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
				this.ServerSetStalk(stalkActive: false);
			}
		}
		else
		{
			this.UpdateMovementState();
		}
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
			Scp106Hud.PlayFlash(vigor: false);
		}
		base.ClientSendCmd();
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this._sinkhole.IsDuringAnimation || !this._sinkhole.ReadonlyCooldown.IsReady || !base.CastRole.FpcModule.IsGrounded)
		{
			return;
		}
		if (this.StalkActive)
		{
			this.ServerSetStalk(stalkActive: false);
		}
		else if (base.VigorAmount < 0.35f)
		{
			if (base.Role.IsLocalPlayer)
			{
				Scp106Hud.PlayFlash(vigor: true);
			}
			this._rpcType = RpcType.NotEnoughVigor;
			base.ServerSendRpc(toAll: false);
		}
		else
		{
			this.ServerSetStalk(stalkActive: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._rpcType);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!NetworkServer.active)
		{
			switch ((RpcType)reader.ReadByte())
			{
			case RpcType.NotEnoughVigor:
				Scp106Hud.PlayFlash(vigor: true);
				break;
			case RpcType.StalkActive:
				this.StalkActive = true;
				break;
			case RpcType.StalkInactive:
				this.StalkActive = false;
				break;
			}
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
		if (this.StalkActive != stalkActive)
		{
			Scp106ChangingStalkModeEventArgs e = new Scp106ChangingStalkModeEventArgs(base.Owner, stalkActive);
			Scp106Events.OnChangingStalkMode(e);
			if (e.IsAllowed)
			{
				this.StalkActive = stalkActive;
				Scp106Events.OnChangedStalkMode(new Scp106ChangedStalkModeEventArgs(base.Owner, stalkActive));
				base.Owner.interCoordinator.AddBlocker(this);
				this._rpcType = ((!stalkActive) ? RpcType.StalkInactive : RpcType.StalkActive);
				base.ServerSendRpc(toAll: true);
			}
		}
	}
}
