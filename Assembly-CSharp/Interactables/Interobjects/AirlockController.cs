using System;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class AirlockController : NetworkBehaviour, IServerInteractable, IInteractable
	{
		private void Start()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			DoorEvents.OnDoorAction += this.OnDoorAction;
			DoorEventOpenerExtension.OnDoorsTriggerred += this.EventTriggerred;
		}

		private void EventTriggerred(DoorEventOpenerExtension.OpenerEventType eventType)
		{
			if (eventType > DoorEventOpenerExtension.OpenerEventType.WarheadCancel)
			{
				if (eventType != DoorEventOpenerExtension.OpenerEventType.DeconFinish)
				{
					return;
				}
				this._doorsLocked = true;
				this._doorA.ServerChangeLock(DoorLockReason.DecontLockdown, true);
				this._doorB.ServerChangeLock(DoorLockReason.DecontLockdown, true);
				this._doorA.NetworkTargetState = (this._doorB.NetworkTargetState = false);
				this._lockdownCombinedTimer = 65535f;
				return;
			}
			else
			{
				this._warheadInProgress = eventType == DoorEventOpenerExtension.OpenerEventType.WarheadStart;
				this._doorA.ServerChangeLock(DoorLockReason.Warhead, this._warheadInProgress);
				this._doorB.ServerChangeLock(DoorLockReason.Warhead, this._warheadInProgress);
				if (this._warheadInProgress)
				{
					this._doorA.NetworkTargetState = (this._doorB.NetworkTargetState = true);
					this._frameCooldownTimer = 5;
					return;
				}
				this.ToggleAirlock();
				return;
			}
		}

		private void OnDestroy()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			DoorEvents.OnDoorAction -= this.OnDoorAction;
			DoorEventOpenerExtension.OnDoorsTriggerred -= this.EventTriggerred;
		}

		private void OnDoorAction(DoorVariant door, DoorAction action, ReferenceHub ply)
		{
			if (door.ActiveLocks > 0 || (door != this._doorA && door != this._doorB) || this.AirlockDisabled || this._warheadInProgress || !this._readyToUse)
			{
				return;
			}
			if (action == DoorAction.Destroyed)
			{
				this.AirlockDisabled = true;
				return;
			}
			if (!this._doorA.AllowInteracting(ply, 0) && !this._doorB.AllowInteracting(ply, 0))
			{
				return;
			}
			if (this._frameCooldownTimer > 0)
			{
				return;
			}
			if (action == DoorAction.Opened || action == DoorAction.Closed)
			{
				this.ToggleAirlock();
			}
		}

		private void ToggleAirlock()
		{
			this._targetStateA = !this._targetStateA;
			this._doorB.NetworkTargetState = this._targetStateA;
			this._doorA.NetworkTargetState = !this._targetStateA;
			this._frameCooldownTimer = 5;
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (this._frameCooldownTimer > 0)
			{
				this._frameCooldownTimer -= 1;
			}
			if (this._readyToUse)
			{
				if (this._lockdownCombinedTimer <= -Mathf.Abs(this._lockdownCooldown))
				{
					return;
				}
				this._lockdownCombinedTimer -= Time.deltaTime;
				if (!this._doorsLocked || this._lockdownCombinedTimer > 0f)
				{
					return;
				}
				this._doorsLocked = false;
				this._doorA.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
				this._doorB.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
				this._doorA.NetworkTargetState = this._targetStateA;
				this._doorB.NetworkTargetState = !this._targetStateA;
				return;
			}
			else
			{
				if (this._frameCooldownTimer != 0)
				{
					return;
				}
				if (Mathf.RoundToInt(this._doorA.GetExactState()) == Mathf.RoundToInt(this._doorB.GetExactState()))
				{
					this._doorB.NetworkTargetState = this._targetStateA;
					this._doorA.NetworkTargetState = !this._targetStateA;
					this._frameCooldownTimer = 200;
					return;
				}
				this._readyToUse = true;
				this._frameCooldownTimer = 10;
				return;
			}
		}

		[ClientRpc]
		private void RpcAlarm()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Interactables.Interobjects.AirlockController::RpcAlarm()", -536901961, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (this.AirlockDisabled || this._lockdownCombinedTimer > -Mathf.Abs(this._lockdownCooldown))
			{
				return;
			}
			this._lockdownCombinedTimer = Mathf.Abs(this._lockdownDuration);
			this._doorsLocked = true;
			this._doorA.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
			this._doorB.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
			this._doorA.NetworkTargetState = (this._doorB.NetworkTargetState = false);
			this.RpcAlarm();
		}

		static AirlockController()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(AirlockController), "System.Void Interactables.Interobjects.AirlockController::RpcAlarm()", new RemoteCallDelegate(AirlockController.InvokeUserCode_RpcAlarm));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcAlarm()
		{
			this._targetAnimator.SetTrigger(AirlockController.AnimationTriggerHash);
		}

		protected static void InvokeUserCode_RpcAlarm(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcAlarm called on server.");
				return;
			}
			((AirlockController)obj).UserCode_RpcAlarm();
		}

		private static readonly int AnimationTriggerHash = Animator.StringToHash("Lockdown");

		public bool AirlockDisabled;

		[SerializeField]
		private DoorVariant _doorA;

		[SerializeField]
		private DoorVariant _doorB;

		[SerializeField]
		private float _lockdownDuration;

		[SerializeField]
		private float _lockdownCooldown;

		[SerializeField]
		private Animator _targetAnimator;

		private float _lockdownCombinedTimer;

		private byte _frameCooldownTimer;

		private bool _targetStateA;

		private bool _doorsLocked;

		private bool _warheadInProgress;

		private bool _readyToUse;
	}
}
