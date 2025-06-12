using CustomPlayerEffects;
using Hazards;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173TantrumAbility : KeySubroutine<Scp173Role>
{
	private const float StainedKillReward = 400f;

	private const float CooldownTime = 30f;

	private const float RayMaxDistance = 3f;

	private const float TantrumHeight = 1.25f;

	public readonly DynamicAbilityCooldown Cooldown = new DynamicAbilityCooldown();

	[SerializeField]
	private TantrumEnvironmentalHazard _tantrumPrefab;

	[SerializeField]
	private LayerMask _tantrumMask;

	private Scp173ObserversTracker _observersTracker;

	private Scp173BlinkTimer _blinkTimer;

	protected override ActionName TargetKey => ActionName.ToggleFlashlight;

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		base.ClientSendCmd();
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp173BlinkTimer>(out this._blinkTimer);
		base.GetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		if (!this.Cooldown.IsReady || this._blinkTimer.RemainingSustainPercent > 0f || this._observersTracker.IsObserved || !Physics.Raycast(base.CastRole.FpcModule.Position, Vector3.down, out var hitInfo, 3f, this._tantrumMask))
		{
			return;
		}
		Scp173CreatingTantrumEventArgs e = new Scp173CreatingTantrumEventArgs(base.Owner);
		Scp173Events.OnCreatingTantrum(e);
		if (!e.IsAllowed)
		{
			return;
		}
		this.Cooldown.Trigger(30.0);
		base.ServerSendRpc(toAll: true);
		TantrumEnvironmentalHazard tantrumEnvironmentalHazard = Object.Instantiate(this._tantrumPrefab);
		Vector3 targetPos = hitInfo.point + Vector3.up * 1.25f;
		tantrumEnvironmentalHazard.SynchronizedPosition = new RelativePosition(targetPos);
		foreach (TeslaGate allGate in TeslaGate.AllGates)
		{
			if (allGate.IsInIdleRange(base.Owner))
			{
				allGate.TantrumsToBeDestroyed.Add(tantrumEnvironmentalHazard);
			}
		}
		Scp173Events.OnCreatedTantrum(new Scp173CreatedTantrumEventArgs(tantrumEnvironmentalHazard, base.Owner));
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		this.Cooldown.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		this.Cooldown.ReadCooldown(reader);
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		PlayerStats.OnAnyPlayerDied += CheckDeath;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
		PlayerStats.OnAnyPlayerDied -= CheckDeath;
	}

	private void CheckDeath(ReferenceHub ply, DamageHandlerBase handler)
	{
		if (NetworkServer.active && handler is ScpDamageHandler scpDamageHandler && !(scpDamageHandler.Attacker.Hub != base.Owner) && ply.playerEffectsController.TryGetEffect<Stained>(out var playerEffect) && playerEffect.IsEnabled)
		{
			HumeShieldModuleBase humeShieldModule = base.CastRole.HumeShieldModule;
			humeShieldModule.HsCurrent = Mathf.Min(humeShieldModule.HsMax, humeShieldModule.HsCurrent + 400f);
		}
	}
}
