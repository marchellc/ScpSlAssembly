using System;
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

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173TantrumAbility : KeySubroutine<Scp173Role>
	{
		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.ToggleFlashlight;
			}
		}

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
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			if (this._blinkTimer.RemainingSustainPercent > 0f || this._observersTracker.IsObserved)
			{
				return;
			}
			RaycastHit raycastHit;
			if (!Physics.Raycast(base.CastRole.FpcModule.Position, Vector3.down, out raycastHit, 3f, this._tantrumMask))
			{
				return;
			}
			Scp173CreatingTantrumEventArgs scp173CreatingTantrumEventArgs = new Scp173CreatingTantrumEventArgs(base.Owner);
			Scp173Events.OnCreatingTantrum(scp173CreatingTantrumEventArgs);
			if (!scp173CreatingTantrumEventArgs.IsAllowed)
			{
				return;
			}
			this.Cooldown.Trigger(30.0);
			base.ServerSendRpc(true);
			TantrumEnvironmentalHazard tantrumEnvironmentalHazard = global::UnityEngine.Object.Instantiate<TantrumEnvironmentalHazard>(this._tantrumPrefab);
			Vector3 vector = raycastHit.point + Vector3.up * 1.25f;
			tantrumEnvironmentalHazard.SynchronizedPosition = new RelativePosition(vector);
			NetworkServer.Spawn(tantrumEnvironmentalHazard.gameObject, null);
			foreach (TeslaGate teslaGate in TeslaGate.AllGates)
			{
				if (teslaGate.IsInIdleRange(base.Owner))
				{
					teslaGate.TantrumsToBeDestroyed.Add(tantrumEnvironmentalHazard);
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
			PlayerStats.OnAnyPlayerDied += this.CheckDeath;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
			PlayerStats.OnAnyPlayerDied -= this.CheckDeath;
		}

		private void CheckDeath(ReferenceHub ply, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ScpDamageHandler scpDamageHandler = handler as ScpDamageHandler;
			if (scpDamageHandler == null)
			{
				return;
			}
			if (scpDamageHandler.Attacker.Hub != base.Owner)
			{
				return;
			}
			Stained stained;
			if (!ply.playerEffectsController.TryGetEffect<Stained>(out stained))
			{
				return;
			}
			if (!stained.IsEnabled)
			{
				return;
			}
			HumeShieldModuleBase humeShieldModule = base.CastRole.HumeShieldModule;
			humeShieldModule.HsCurrent = Mathf.Min(humeShieldModule.HsMax, humeShieldModule.HsCurrent + 400f);
		}

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
	}
}
