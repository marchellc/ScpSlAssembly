using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096RageManager : StandardSubroutine<Scp096Role>, IHumeShieldBlocker
{
	public const float NormalHumeRegenerationRate = 15f;

	public const float MaxRageTime = 35f;

	public const float MinimumEnrageTime = 20f;

	private const float TimePerExtraTarget = 3f;

	private const float CalmingShieldMultiplier = 1f;

	private const float EnragingShieldMultiplier = 1f;

	public readonly AbilityCooldown HudRageDuration = new AbilityCooldown();

	private DynamicHumeShieldController _shieldController;

	private Scp096TargetsTracker _targetsTracker;

	private float _enragedTimeLeft;

	public bool HumeShieldBlocked { get; set; }

	public bool IsEnragedOrDistressed
	{
		get
		{
			if (!this.IsEnraged)
			{
				return this.IsDistressed;
			}
			return true;
		}
	}

	public bool IsEnraged => base.CastRole.IsRageState(Scp096RageState.Enraged);

	public bool IsDistressed => base.CastRole.IsRageState(Scp096RageState.Distressed);

	public float EnragedTimeLeft
	{
		get
		{
			return this._enragedTimeLeft;
		}
		set
		{
			if (value < 0f)
			{
				value = 0f;
			}
			this.HudRageDuration.Remaining = value;
			this._enragedTimeLeft = value;
			if (NetworkServer.active && this._enragedTimeLeft == 0f)
			{
				this.ServerEndEnrage(clearTime: false);
			}
		}
	}

	public float TotalRageTime { get; private set; }

	public void ServerEnrage(float initialDuration = 20f)
	{
		if (NetworkServer.active)
		{
			Scp096EnragingEventArgs e = new Scp096EnragingEventArgs(base.Owner, initialDuration);
			Scp096Events.OnEnraging(e);
			if (e.IsAllowed)
			{
				initialDuration = e.InitialDuration;
				this.EnragedTimeLeft = initialDuration;
				this.TotalRageTime = initialDuration;
				base.CastRole.StateController.SetRageState(Scp096RageState.Distressed);
				this.ServerIncreaseDuration(base.Owner, Mathf.Max((float)this._targetsTracker.Targets.Count - 3f, 0f));
				Scp096Events.OnEnraged(new Scp096EnragedEventArgs(base.Owner, initialDuration));
			}
		}
	}

	public void ServerEndEnrage(bool clearTime = true)
	{
		if (NetworkServer.active)
		{
			if (clearTime)
			{
				this.EnragedTimeLeft = 0f;
			}
			base.CastRole.StateController.SetRageState(Scp096RageState.Calming);
			base.ServerSendRpc(toAll: true);
		}
	}

	public void ServerIncreaseDuration(ReferenceHub ownerHub, float addedDuration = 3f)
	{
		if (NetworkServer.active && !(ownerHub != base.Owner))
		{
			addedDuration = Mathf.Min(addedDuration, 35f - this.TotalRageTime);
			this.TotalRageTime += addedDuration;
			this.EnragedTimeLeft += addedDuration;
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteFloat(this.EnragedTimeLeft);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!NetworkServer.active)
		{
			this.EnragedTimeLeft = reader.ReadFloat();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._shieldController = base.CastRole.HumeShieldModule as DynamicHumeShieldController;
		base.GetSubroutine<Scp096TargetsTracker>(out this._targetsTracker);
		Scp096TargetsTracker.OnTargetAdded += delegate(ReferenceHub ownerHub, ReferenceHub targetedHub)
		{
			this.ServerIncreaseDuration(ownerHub);
		};
		base.CastRole.StateController.OnRageUpdate += OnRageUpdate;
	}

	private void OnRageUpdate(Scp096RageState newState)
	{
		if (newState == Scp096RageState.Enraged)
		{
			this.HudRageDuration.Trigger(this.EnragedTimeLeft);
		}
		if (NetworkServer.active)
		{
			float num;
			switch (newState)
			{
			case Scp096RageState.Enraged:
				num = 1f;
				this.HumeShieldBlocked = true;
				this._shieldController.AddBlocker(this);
				break;
			case Scp096RageState.Calming:
				num = 1f;
				this.TotalRageTime = 0f;
				this.HumeShieldBlocked = false;
				break;
			default:
				this.HumeShieldBlocked = false;
				return;
			}
			HumeShieldModuleBase humeShieldModule = base.CastRole.HumeShieldModule;
			humeShieldModule.HsCurrent = Mathf.Clamp(humeShieldModule.HsCurrent * num, 0f, humeShieldModule.HsMax);
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.UpdateRage();
		}
	}

	private void UpdateRage()
	{
		if (this.IsEnraged)
		{
			this.EnragedTimeLeft -= Time.deltaTime;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.HudRageDuration.Clear();
		this._shieldController.RegenerationRate = 15f;
		this.HumeShieldBlocked = false;
		this._enragedTimeLeft = 0f;
		this.TotalRageTime = 0f;
	}
}
