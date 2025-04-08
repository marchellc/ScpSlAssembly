using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class BasicDoor : DoorVariant
	{
		public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
		{
			return this._remainingAnimCooldown <= 0f;
		}

		public override float GetExactState()
		{
			Vector3 position = this._stateMoveable.position;
			Vector3 position2 = this._stateStator.position;
			float num = Mathf.Abs(position.x - position2.x) + Mathf.Abs(position.y - position2.y) + Mathf.Abs(position.z - position2.z);
			return Mathf.Clamp01(Mathf.InverseLerp(this._stateMinDis, this._stateMaxDis, num));
		}

		public override bool IsConsideredOpen()
		{
			return this.GetExactState() > this._consideredOpenThreshold;
		}

		public override bool AnticheatPassageApproved()
		{
			return this.IsConsideredOpen() || (!this.TargetState && this.GetExactState() > this._anticheatPassableThreshold);
		}

		public override void LockBypassDenied(ReferenceHub ply, byte colliderId)
		{
			this.RpcPlayBeepSound(false);
		}

		public override void PermissionsDenied(ReferenceHub ply, byte colliderId)
		{
			this.RpcPlayBeepSound(true);
		}

		[ClientRpc]
		private void RpcPlayBeepSound(bool setDeniedButtons)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteBool(setDeniedButtons);
			this.SendRPCInternal("System.Void Interactables.Interobjects.BasicDoor::RpcPlayBeepSound(System.Boolean)", 394418581, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		protected override void Update()
		{
			base.Update();
			if (NetworkServer.active && this._remainingAnimCooldown > 0f)
			{
				this._remainingAnimCooldown -= Time.deltaTime;
			}
		}

		internal override void TargetStateChanged()
		{
			this.MainAnimator.SetBool(BasicDoor.AnimHash, this.TargetState);
			if (NetworkServer.active)
			{
				this._remainingAnimCooldown = this._cooldownDuration;
			}
		}

		protected override void LockChanged(ushort prevValue)
		{
			this.UpdateAnimations = true;
		}

		static BasicDoor()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(BasicDoor), "System.Void Interactables.Interobjects.BasicDoor::RpcPlayBeepSound(System.Boolean)", new RemoteCallDelegate(BasicDoor.InvokeUserCode_RpcPlayBeepSound__Boolean));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcPlayBeepSound__Boolean(bool setDeniedButtons)
		{
		}

		protected static void InvokeUserCode_RpcPlayBeepSound__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayBeepSound called on server.");
				return;
			}
			((BasicDoor)obj).UserCode_RpcPlayBeepSound__Boolean(reader.ReadBool());
		}

		private static readonly int AnimHash = Animator.StringToHash("isOpen");

		[Header("General settings")]
		[SerializeField]
		internal Animator MainAnimator;

		[SerializeField]
		internal AudioSource MainSource;

		[SerializeField]
		private float _cooldownDuration;

		[SerializeField]
		private float _consideredOpenThreshold = 0.7f;

		[SerializeField]
		private float _anticheatPassableThreshold = 0.2f;

		[Header("These values are used to get the exact state")]
		[SerializeField]
		private Transform _stateMoveable;

		[SerializeField]
		private Transform _stateStator;

		[SerializeField]
		private float _stateMinDis;

		[SerializeField]
		private float _stateMaxDis;

		[HideInInspector]
		public bool UpdateAnimations;

		public List<Collider> Scp106Colliders;

		private float _remainingAnimCooldown;
	}
}
