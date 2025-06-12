using CursorManagement;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class Strangled : StatusEffectBase, ISoundtrackMutingEffect, IMovementInputOverride, IStaminaModifier, IMovementSpeedModifier, ICursorOverride, IInteractionBlocker
{
	private bool _hasCache;

	private bool _overrideRegistered;

	private double _startTime;

	private float _remainingLineOfSightTolerance;

	private ReferenceHub _cachedHub;

	private Scp3114Strangle.StrangleTarget _strangleTarget;

	private const float MaxMovementSpeed = 5f;

	private const float LookLerp = 20f;

	private const float StartDamageRate = 5f;

	private const float DamageRatePerSec = 5f;

	private const float LineOfSightToleranceSeconds = 0.5f;

	public bool MuteSoundtrack => base.IsEnabled;

	public bool MovementOverrideActive => base.IsEnabled;

	public Vector3 MovementOverrideDirection { get; private set; }

	public bool StaminaModifierActive => base.IsEnabled;

	public bool SprintingDisabled => base.IsEnabled;

	public bool MovementModifierActive
	{
		get
		{
			if (base.IsEnabled)
			{
				return base.IsLocalPlayer;
			}
			return false;
		}
	}

	public float MovementSpeedMultiplier => 2.1474836E+09f;

	public float MovementSpeedLimit { get; private set; }

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement => base.IsEnabled;

	public override bool AllowEnabling => !SpawnProtected.CheckPlayer(base.Hub);

	private float DamageRate
	{
		get
		{
			double elapsedSeconds = this.ElapsedSeconds;
			if (elapsedSeconds < 0.0)
			{
				return 0f;
			}
			double num = 5.0 * elapsedSeconds;
			return (float)(5.0 + num);
		}
	}

	public float EstimatedTimeToKill
	{
		get
		{
			HealthStat module = base.Hub.playerStats.GetModule<HealthStat>();
			AhpStat module2 = base.Hub.playerStats.GetModule<AhpStat>();
			float num = module.CurValue + module2.CurValue;
			float damageRate = this.DamageRate;
			float num2 = damageRate * damageRate;
			return (0f - damageRate + Mathf.Sqrt(num2 + 10f * num)) / 5f;
		}
	}

	public double ElapsedSeconds => NetworkTime.time - this._startTime;

	public BlockedInteraction BlockedInteractions => BlockedInteraction.ItemUsage | BlockedInteraction.OpenInventory;

	public bool CanBeCleared => !base.IsEnabled;

	protected override void Enabled()
	{
		base.Enabled();
		this._startTime = NetworkTime.time;
		this._remainingLineOfSightTolerance = 0.5f;
		this.MovementSpeedLimit = 0f;
		this.MovementOverrideDirection = Vector3.zero;
		base.Hub.interCoordinator.AddBlocker(this);
		if (base.Hub.isLocalPlayer)
		{
			this._overrideRegistered = true;
			CursorManager.Register(this);
		}
	}

	protected override void Disabled()
	{
		base.Disabled();
		if (base.Hub.isLocalPlayer)
		{
			this._overrideRegistered = false;
			CursorManager.Unregister(this);
		}
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		if (base.IsLocalPlayer)
		{
			this.UpdateInputOverride();
		}
		if (NetworkServer.active && !this.ServerUpdate())
		{
			this.DisableEffect();
		}
	}

	private bool ServerUpdate()
	{
		if (!this.TryUpdateAttacker(out var attacker))
		{
			return false;
		}
		Vector3 position = base.Hub.PlayerCameraReference.position;
		Vector3 position2 = attacker.PlayerCameraReference.position;
		if (!Physics.Linecast(position, position2, FpcStateProcessor.Mask))
		{
			this._remainingLineOfSightTolerance = 0.5f;
		}
		else
		{
			this._remainingLineOfSightTolerance -= Time.deltaTime;
			if (this._remainingLineOfSightTolerance < 0f)
			{
				return false;
			}
		}
		base.Hub.inventory.ServerSelectItem(0);
		base.Hub.playerStats.DealDamage(new Scp3114DamageHandler(attacker, this.DamageRate * Time.deltaTime, Scp3114DamageHandler.HandlerType.Strangulation));
		return true;
	}

	private void UpdateInputOverride()
	{
		if (this.TryUpdateAttacker(out var _) && base.Hub.roleManager.CurrentRole is IFpcRole { FpcModule: var fpcModule } fpcRole)
		{
			fpcRole.LookAtPoint(this._strangleTarget.AttackerPosition.Position, Time.deltaTime * 20f);
			Vector3 vector = this._strangleTarget.TargetPosition.Position - fpcModule.Position;
			float num = vector.MagnitudeIgnoreY();
			Vector3 movementOverrideDirection = vector / num;
			if (!Mathf.Approximately(num, 0f))
			{
				this.MovementOverrideDirection = movementOverrideDirection;
				this.MovementSpeedLimit = Mathf.Min(5f, num / Time.deltaTime);
			}
		}
	}

	public bool TryUpdateAttacker(out ReferenceHub attacker)
	{
		if (this._hasCache && this._cachedHub != null && CheckPlayer(this._cachedHub))
		{
			attacker = this._cachedHub;
			return true;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (CheckPlayer(allHub))
			{
				attacker = allHub;
				this._hasCache = true;
				return true;
			}
		}
		this._hasCache = false;
		attacker = null;
		return false;
		bool CheckPlayer(ReferenceHub hub)
		{
			if (!(hub.roleManager.CurrentRole is Scp3114Role scp3114Role))
			{
				return false;
			}
			if (!scp3114Role.SubroutineModule.TryGetSubroutine<Scp3114Strangle>(out var subroutine))
			{
				return false;
			}
			if (!subroutine.SyncTarget.HasValue)
			{
				return false;
			}
			if (subroutine.SyncTarget.Value.Target != base.Hub)
			{
				return false;
			}
			this._cachedHub = hub;
			this._strangleTarget = subroutine.SyncTarget.Value;
			return true;
		}
	}

	private void OnDestroy()
	{
		if (this._overrideRegistered)
		{
			CursorManager.Unregister(this);
		}
	}
}
