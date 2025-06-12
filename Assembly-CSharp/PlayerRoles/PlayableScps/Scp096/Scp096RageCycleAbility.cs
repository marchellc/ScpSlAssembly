using GameObjectPools;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096RageCycleAbility : KeySubroutine<Scp096Role>, IPoolResettable
{
	public const ActionName RageKey = ActionName.Reload;

	private const float EnragingTime = 6.1f;

	private const float CalmingTime = 5f;

	private const float DefaultActivationDuration = 10f;

	private const float RateCompensation = 0.2f;

	private const float KeyHoldTime = 0.4f;

	private readonly AbilityCooldown _activationTime = new AbilityCooldown();

	private Scp096RageManager _rageManager;

	private Scp096TargetsTracker _targetsTracker;

	private float _holdingRageCycleKey;

	private bool _wantsToToggle;

	private float _timeToChangeState;

	public float HudEnterRageSustain => Mathf.InverseLerp(0.4f, 9.8f, this._activationTime.Remaining);

	public float HudEnterRageKeyProgress => Mathf.Clamp01(this._holdingRageCycleKey / 0.4f);

	public bool CanStartCycle
	{
		get
		{
			if (!this._activationTime.IsReady)
			{
				return base.CastRole.IsRageState(Scp096RageState.Docile);
			}
			return false;
		}
	}

	public bool CanEndCycle => base.CastRole.IsRageState(Scp096RageState.Enraged);

	protected override ActionName TargetKey => ActionName.Reload;

	public bool ServerTryEnableInput(float duration = 10f)
	{
		this._activationTime.Trigger(duration);
		base.ServerSendRpc(toAll: true);
		return true;
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.CanStartCycle)
		{
			this._rageManager.ServerEnrage();
		}
		else if (this.CanEndCycle)
		{
			this._rageManager.ServerEndEnrage();
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this._activationTime.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._activationTime.ReadCooldown(reader);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._activationTime.Clear();
		this._holdingRageCycleKey = 0f;
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		this._wantsToToggle = true;
	}

	protected override void Awake()
	{
		base.Awake();
		base.CastRole.SubroutineModule.TryGetSubroutine<Scp096TargetsTracker>(out this._targetsTracker);
		base.CastRole.SubroutineModule.TryGetSubroutine<Scp096RageManager>(out this._rageManager);
		Scp096TargetsTracker.OnTargetAdded += AddTarget;
		this._targetsTracker.OnTargetAttacked += delegate(ReferenceHub targetedHub)
		{
			this.AddTarget(base.Owner, targetedHub);
		};
		base.CastRole.StateController.OnRageUpdate += delegate(Scp096RageState newState)
		{
			switch (newState)
			{
			case Scp096RageState.Calming:
				this._timeToChangeState = 5f;
				break;
			case Scp096RageState.Distressed:
				this._timeToChangeState = 6.1f;
				break;
			}
		};
	}

	protected override void Update()
	{
		base.Update();
		if (this._wantsToToggle)
		{
			this.UpdateKeyHeld();
		}
		if (NetworkServer.active)
		{
			this.UpdateServerside();
		}
	}

	private void UpdateKeyHeld()
	{
		if ((!this.CanEndCycle && !this.CanStartCycle) || !this.IsKeyHeld)
		{
			this._holdingRageCycleKey = 0f;
			this._wantsToToggle = false;
			return;
		}
		this._holdingRageCycleKey += Time.deltaTime;
		if (this._holdingRageCycleKey >= 0.4f)
		{
			base.ClientSendCmd();
			this._holdingRageCycleKey = 0f;
		}
	}

	private void UpdateServerside()
	{
		switch (base.CastRole.StateController.RageState)
		{
		case Scp096RageState.Enraged:
			return;
		case Scp096RageState.Distressed:
			this._timeToChangeState -= Time.deltaTime;
			if (this._timeToChangeState < 0f)
			{
				base.CastRole.StateController.SetRageState(Scp096RageState.Enraged);
			}
			return;
		case Scp096RageState.Calming:
			this._timeToChangeState -= Time.deltaTime;
			if (this._timeToChangeState < 0f)
			{
				base.CastRole.StateController.SetRageState(Scp096RageState.Docile);
			}
			return;
		}
		foreach (ReferenceHub target in this._targetsTracker.Targets)
		{
			if (this._targetsTracker.IsObservedBy(target))
			{
				this.ServerTryEnableInput();
			}
		}
		if (this._activationTime.IsReady && this._targetsTracker.Targets.Count > 0)
		{
			this._targetsTracker.ClearAllTargets();
			base.ServerSendRpc(toAll: true);
		}
	}

	private void AddTarget(ReferenceHub ownerHub, ReferenceHub targetedHub)
	{
		if (NetworkServer.active && !(ownerHub != base.Owner))
		{
			this.ServerTryEnableInput();
		}
	}
}
