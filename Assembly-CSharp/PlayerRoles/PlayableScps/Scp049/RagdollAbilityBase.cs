using System;
using CursorManagement;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049
{
	public abstract class RagdollAbilityBase<T> : KeySubroutine<T>, ICursorOverride where T : FpcStandardScp
	{
		public virtual CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		public virtual bool LockMovement
		{
			get
			{
				return this.IsInProgress && base.Owner.isLocalPlayer;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Interact;
			}
		}

		public event Action<byte> OnErrorReceived;

		public event Action OnStop;

		public event Action OnStart;

		public bool IsInProgress
		{
			get
			{
				return this._completionTime != 0.0;
			}
			private set
			{
				this._completionTime = (value ? (NetworkTime.time + (double)this.Duration) : 0.0);
				base.ServerSendRpc(true);
			}
		}

		public float ProgressStatus
		{
			get
			{
				return this._process.Readiness;
			}
		}

		protected abstract float RangeSqr { get; }

		protected abstract float Duration { get; }

		private protected BasicRagdoll CurRagdoll { protected get; private set; }

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
				if (!this.IsInProgress)
				{
					return;
				}
				this._errorCode = this.ServerValidateCancel();
				if (this._errorCode != 0)
				{
					base.ServerSendRpc(true);
					return;
				}
				this.IsInProgress = false;
				return;
			}
			else
			{
				if (this.IsInProgress)
				{
					return;
				}
				Transform transform;
				if (!this.IsCorpseNearby(position, this._syncRagdoll, out transform))
				{
					return;
				}
				Transform ragdollTransform = this._ragdollTransform;
				BasicRagdoll curRagdoll = this.CurRagdoll;
				this._ragdollTransform = transform;
				this.CurRagdoll = this._syncRagdoll;
				this._errorCode = this.ServerValidateBegin(this._syncRagdoll);
				bool flag = this._errorCode > 0;
				if (flag || !this.ServerValidateAny())
				{
					this._ragdollTransform = ragdollTransform;
					this.CurRagdoll = curRagdoll;
					if (flag)
					{
						base.ServerSendRpc(true);
					}
					return;
				}
				this.IsInProgress = true;
				return;
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
					Action onStart = this.OnStart;
					if (onStart != null)
					{
						onStart();
					}
				}
				if (completionTime != 0.0)
				{
					Action onStop = this.OnStop;
					if (onStop != null)
					{
						onStop();
					}
				}
			}
			if (base.Owner.isLocalPlayer && b != 0)
			{
				Action<byte> onErrorReceived = this.OnErrorReceived;
				if (onErrorReceived != null)
				{
					onErrorReceived(b);
				}
				this.ClientProcessErrorCode(b);
			}
			this.OnProgressSet();
			if (!this.IsInProgress)
			{
				this._process.Clear();
				return;
			}
			if (!flag)
			{
				this._process.Trigger((double)((float)(this._completionTime - NetworkTime.time)));
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
			if (!NetworkServer.active || !this.IsInProgress)
			{
				return;
			}
			if (!this.ServerValidateAny())
			{
				this.IsInProgress = false;
				return;
			}
			if (NetworkTime.time < this._completionTime)
			{
				return;
			}
			this.ServerComplete();
			this.IsInProgress = false;
		}

		protected void ClientTryStart()
		{
			BasicRagdoll basicRagdoll;
			if (!this.CanFindCorpse(base.Owner.PlayerCameraReference, out basicRagdoll))
			{
				return;
			}
			if (this.ClientValidateBegin(basicRagdoll))
			{
				DynamicRagdoll dynamicRagdoll = basicRagdoll as DynamicRagdoll;
				if (dynamicRagdoll != null)
				{
					Transform transform;
					if (!this.IsCorpseNearby(base.CastRole.FpcModule.Position, dynamicRagdoll, out transform))
					{
						return;
					}
					this._syncRagdoll = dynamicRagdoll;
					base.ClientSendCmd();
					return;
				}
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
			foreach (Transform transform in ragdoll.LinkedRigidbodiesTransforms)
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
			RaycastHit raycastHit;
			if (!Physics.Raycast(tr.position, tr.forward, out raycastHit, this.RangeSqr))
			{
				return false;
			}
			BasicRagdoll componentInParent;
			ragdoll = (componentInParent = raycastHit.transform.GetComponentInParent<BasicRagdoll>());
			return componentInParent;
		}

		private bool IsCloseEnough(Vector3 position, Vector3 ragdollPosition)
		{
			return (ragdollPosition - position).sqrMagnitude < this.RangeSqr;
		}

		private readonly AbilityCooldown _process = new AbilityCooldown();

		private Transform _ragdollTransform;

		private DynamicRagdoll _syncRagdoll;

		private byte _errorCode;

		private double _completionTime;
	}
}
