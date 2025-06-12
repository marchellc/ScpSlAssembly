using System;
using CursorManagement;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049;

public abstract class RagdollAbilityBase<T> : KeySubroutine<T>, ICursorOverride where T : FpcStandardScp
{
	private static readonly CachedLayerMask RaycastBlockMask = new CachedLayerMask("Default", "InteractableNoPlayerCollision", "Glass", "Ragdoll", "Door");

	private readonly AbilityCooldown _process = new AbilityCooldown();

	private Transform _ragdollTransform;

	private DynamicRagdoll _syncRagdoll;

	private byte _errorCode;

	private double _completionTime;

	public virtual CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public virtual bool LockMovement
	{
		get
		{
			if (this.IsInProgress)
			{
				return base.Owner.isLocalPlayer;
			}
			return false;
		}
	}

	protected override ActionName TargetKey => ActionName.Interact;

	public bool IsInProgress
	{
		get
		{
			return this._completionTime != 0.0;
		}
		private set
		{
			this._completionTime = (value ? (NetworkTime.time + (double)this.Duration) : 0.0);
			base.ServerSendRpc(toAll: true);
		}
	}

	public float ProgressStatus => this._process.Readiness;

	protected abstract float RangeSqr { get; }

	protected abstract float Duration { get; }

	protected BasicRagdoll CurRagdoll { get; private set; }

	public event Action<byte> OnErrorReceived;

	public event Action OnStop;

	public event Action OnStart;

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteNetworkBehaviour(this._syncRagdoll);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		Vector3 position = base.CastRole.FpcModule.Position;
		this._syncRagdoll = reader.ReadNetworkBehaviour<DynamicRagdoll>();
		if (this._syncRagdoll == null)
		{
			if (this.IsInProgress)
			{
				this._errorCode = this.ServerValidateCancel();
				if (this._errorCode != 0)
				{
					base.ServerSendRpc(toAll: true);
				}
				else
				{
					this.IsInProgress = false;
				}
			}
		}
		else
		{
			if (this.IsInProgress || !this.IsCorpseNearby(position, this._syncRagdoll, out var ragdollPosition))
			{
				return;
			}
			Transform ragdollTransform = this._ragdollTransform;
			BasicRagdoll curRagdoll = this.CurRagdoll;
			this._ragdollTransform = ragdollPosition;
			this.CurRagdoll = this._syncRagdoll;
			this._errorCode = this.ServerValidateBegin(this._syncRagdoll);
			bool flag = this._errorCode != 0;
			if (flag || !this.ServerValidateAny())
			{
				this._ragdollTransform = ragdollTransform;
				this.CurRagdoll = curRagdoll;
				if (flag)
				{
					base.ServerSendRpc(toAll: true);
				}
			}
			else
			{
				this.IsInProgress = true;
			}
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteNetworkBehaviour(this.CurRagdoll);
		writer.WriteDouble(this._completionTime);
		writer.WriteByte(this._errorCode);
		this._errorCode = 0;
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		bool flag = this.IsInProgress && !NetworkServer.active;
		double completionTime = this._completionTime;
		this.CurRagdoll = reader.ReadNetworkBehaviour<BasicRagdoll>();
		this._completionTime = reader.ReadDouble();
		byte b = reader.ReadByte();
		if (this._completionTime != completionTime)
		{
			if (completionTime == 0.0)
			{
				this.OnStart?.Invoke();
			}
			if (completionTime != 0.0)
			{
				this.OnStop?.Invoke();
			}
		}
		if (base.Owner.isLocalPlayer && b != 0)
		{
			this.OnErrorReceived?.Invoke(b);
			this.ClientProcessErrorCode(b);
		}
		this.OnProgressSet();
		if (!this.IsInProgress)
		{
			this._process.Clear();
		}
		else if (!flag)
		{
			this._process.Trigger((float)(this._completionTime - NetworkTime.time));
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (base.Owner.isLocalPlayer)
		{
			CursorManager.Register(this);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		CursorManager.Unregister(this);
		this._completionTime = 0.0;
		this._process.Clear();
	}

	protected abstract void ServerComplete();

	protected abstract byte ServerValidateBegin(BasicRagdoll ragdoll);

	protected virtual void OnProgressSet()
	{
	}

	protected virtual byte ServerValidateCancel()
	{
		return 0;
	}

	protected virtual bool ServerValidateAny()
	{
		return this.IsCloseEnough(base.CastRole.FpcModule.Position, this._ragdollTransform.position);
	}

	protected virtual void ClientProcessErrorCode(byte code)
	{
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active && this.IsInProgress)
		{
			if (!this.ServerValidateAny())
			{
				this.IsInProgress = false;
			}
			else if (!(NetworkTime.time < this._completionTime))
			{
				this.ServerComplete();
				this.IsInProgress = false;
			}
		}
	}

	protected void ClientTryStart()
	{
		if (this.CanFindCorpse(base.Owner.PlayerCameraReference, out var ragdoll) && this.ClientValidateBegin(ragdoll) && ragdoll is DynamicRagdoll dynamicRagdoll && this.IsCorpseNearby(base.CastRole.FpcModule.Position, dynamicRagdoll, out var _))
		{
			this._syncRagdoll = dynamicRagdoll;
			base.ClientSendCmd();
		}
	}

	protected void ClientTryCancel()
	{
		this._syncRagdoll = null;
		base.ClientSendCmd();
	}

	protected virtual bool ClientValidateBegin(BasicRagdoll raycastedRagdoll)
	{
		return true;
	}

	private bool IsCorpseNearby(Vector3 position, DynamicRagdoll ragdoll, out Transform ragdollPosition)
	{
		Transform[] linkedRigidbodiesTransforms = ragdoll.LinkedRigidbodiesTransforms;
		foreach (Transform transform in linkedRigidbodiesTransforms)
		{
			if (this.IsCloseEnough(position, transform.position))
			{
				ragdollPosition = transform.transform;
				return true;
			}
		}
		ragdollPosition = ragdoll.transform;
		return false;
	}

	private bool CanFindCorpse(Transform tr, out BasicRagdoll ragdoll)
	{
		ragdoll = null;
		if (!Physics.Raycast(tr.position, tr.forward, out var hitInfo, this.RangeSqr, RagdollAbilityBase<T>.RaycastBlockMask))
		{
			return false;
		}
		return hitInfo.transform.TryGetComponentInParent<BasicRagdoll>(out ragdoll);
	}

	private bool IsCloseEnough(Vector3 position, Vector3 ragdollPosition)
	{
		return (ragdollPosition - position).sqrMagnitude < this.RangeSqr;
	}
}
