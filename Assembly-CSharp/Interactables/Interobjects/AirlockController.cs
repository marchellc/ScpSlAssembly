using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects;

public class AirlockController : NetworkBehaviour, IServerInteractable, IInteractable
{
	private static readonly int AnimationTriggerHash;

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

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	private void Start()
	{
		if (NetworkServer.active)
		{
			DoorEvents.OnDoorAction += OnDoorAction;
			DoorEventOpenerExtension.OnDoorsTriggerred += EventTriggerred;
		}
	}

	private void EventTriggerred(DoorEventOpenerExtension.OpenerEventType eventType)
	{
		switch (eventType)
		{
		case DoorEventOpenerExtension.OpenerEventType.WarheadStart:
		case DoorEventOpenerExtension.OpenerEventType.WarheadCancel:
			this._warheadInProgress = eventType == DoorEventOpenerExtension.OpenerEventType.WarheadStart;
			this._doorA.ServerChangeLock(DoorLockReason.Warhead, this._warheadInProgress);
			this._doorB.ServerChangeLock(DoorLockReason.Warhead, this._warheadInProgress);
			if (this._warheadInProgress)
			{
				DoorVariant doorA2 = this._doorA;
				bool networkTargetState = (this._doorB.NetworkTargetState = true);
				doorA2.NetworkTargetState = networkTargetState;
				this._frameCooldownTimer = 5;
			}
			else
			{
				this.ToggleAirlock();
			}
			break;
		case DoorEventOpenerExtension.OpenerEventType.DeconFinish:
		{
			this._doorsLocked = true;
			this._doorA.ServerChangeLock(DoorLockReason.DecontLockdown, newState: true);
			this._doorB.ServerChangeLock(DoorLockReason.DecontLockdown, newState: true);
			DoorVariant doorA = this._doorA;
			bool networkTargetState = (this._doorB.NetworkTargetState = false);
			doorA.NetworkTargetState = networkTargetState;
			this._lockdownCombinedTimer = 65535f;
			break;
		}
		}
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			DoorEvents.OnDoorAction -= OnDoorAction;
			DoorEventOpenerExtension.OnDoorsTriggerred -= EventTriggerred;
		}
	}

	private void OnDoorAction(DoorVariant door, DoorAction action, ReferenceHub ply)
	{
		if (door.ActiveLocks <= 0 && (!(door != this._doorA) || !(door != this._doorB)) && !this.AirlockDisabled && !this._warheadInProgress && this._readyToUse)
		{
			if (action == DoorAction.Destroyed)
			{
				this.AirlockDisabled = true;
			}
			else if ((this._doorA.AllowInteracting(ply, 0) || this._doorB.AllowInteracting(ply, 0)) && this._frameCooldownTimer <= 0 && (action == DoorAction.Opened || action == DoorAction.Closed))
			{
				this.ToggleAirlock();
			}
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
			this._frameCooldownTimer--;
		}
		if (this._readyToUse)
		{
			if (this._lockdownCombinedTimer > 0f - Mathf.Abs(this._lockdownCooldown))
			{
				this._lockdownCombinedTimer -= Time.deltaTime;
				if (this._doorsLocked && this._lockdownCombinedTimer <= 0f)
				{
					this._doorsLocked = false;
					this._doorA.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: false);
					this._doorB.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: false);
					this._doorA.NetworkTargetState = this._targetStateA;
					this._doorB.NetworkTargetState = !this._targetStateA;
				}
			}
		}
		else if (this._frameCooldownTimer == 0)
		{
			if (Mathf.RoundToInt(this._doorA.GetExactState()) == Mathf.RoundToInt(this._doorB.GetExactState()))
			{
				this._doorB.NetworkTargetState = this._targetStateA;
				this._doorA.NetworkTargetState = !this._targetStateA;
				this._frameCooldownTimer = 200;
			}
			else
			{
				this._readyToUse = true;
				this._frameCooldownTimer = 10;
			}
		}
	}

	[ClientRpc]
	private void RpcAlarm()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Interactables.Interobjects.AirlockController::RpcAlarm()", -536901961, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!this.AirlockDisabled && !(this._lockdownCombinedTimer > 0f - Mathf.Abs(this._lockdownCooldown)))
		{
			this._lockdownCombinedTimer = Mathf.Abs(this._lockdownDuration);
			this._doorsLocked = true;
			this._doorA.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: true);
			this._doorB.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: true);
			DoorVariant doorA = this._doorA;
			bool networkTargetState = (this._doorB.NetworkTargetState = false);
			doorA.NetworkTargetState = networkTargetState;
			this.RpcAlarm();
		}
	}

	static AirlockController()
	{
		AirlockController.AnimationTriggerHash = Animator.StringToHash("Lockdown");
		RemoteProcedureCalls.RegisterRpc(typeof(AirlockController), "System.Void Interactables.Interobjects.AirlockController::RpcAlarm()", InvokeUserCode_RpcAlarm);
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
		}
		else
		{
			((AirlockController)obj).UserCode_RpcAlarm();
		}
	}
}
