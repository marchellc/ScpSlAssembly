using System.Collections.Generic;
using Mirror;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507SwarmAbility : StandardSubroutine<Scp1507Role>
{
	private const float RegenCombatCooldown = 0.7f;

	private const float EffectRange = 7.5f;

	private const float BaseHealthRegenRate = 1f;

	private const float EffectRangeSqr = 56.25f;

	private const float AdjustSpeed = 0.6f;

	private readonly HashSet<Scp1507Role> _nearbyFlamingos = new HashSet<Scp1507Role>();

	private readonly HashSet<Scp1507Role> _entireFlock = new HashSet<Scp1507Role>();

	private byte _flockSize;

	private float _regenCooldown;

	private HealthStat _healthStat;

	public int FlockSize => this._flockSize;

	public float Multiplier { get; private set; }

	private void UpdateMultiplier()
	{
		this.Multiplier = Mathf.MoveTowards(this.Multiplier, this.FlockSize, 0.6f * Time.deltaTime);
	}

	private void Update()
	{
		this.UpdateMultiplier();
		if (NetworkServer.active)
		{
			this.ServerUpdateNearby();
			this.ServerUpdateFlock();
			this.ServerUpdateHealthRegen();
		}
	}

	private void ServerUpdateNearby()
	{
		Vector3 position = base.CastRole.FpcModule.Position;
		this._nearbyFlamingos.Clear();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is Scp1507Role { Team: Team.Flamingos } scp1507Role)
			{
				Vector3 position2 = scp1507Role.FpcModule.Position;
				if (!((position - position2).sqrMagnitude > 56.25f))
				{
					this._nearbyFlamingos.Add(scp1507Role);
				}
			}
		}
	}

	private void ServerUpdateFlock()
	{
		this._entireFlock.Clear();
		foreach (Scp1507Role nearbyFlamingo in this._nearbyFlamingos)
		{
			this._entireFlock.Add(nearbyFlamingo);
			if (!nearbyFlamingo.SubroutineModule.TryGetSubroutine<Scp1507SwarmAbility>(out var subroutine))
			{
				continue;
			}
			foreach (Scp1507Role nearbyFlamingo2 in subroutine._nearbyFlamingos)
			{
				this._entireFlock.Add(nearbyFlamingo2);
			}
		}
		int num = Mathf.Max(0, this._entireFlock.Count - 1);
		if (num != this._flockSize)
		{
			this._flockSize = (byte)num;
			base.ServerSendRpc(toAll: true);
		}
	}

	private void ServerUpdateHealthRegen()
	{
		if (this._regenCooldown >= 0.7f)
		{
			this._regenCooldown -= Time.deltaTime;
		}
		else
		{
			this._healthStat.ServerHeal(this.Multiplier * 1f * Time.deltaTime);
		}
	}

	private void OnPlayerDamaged(ReferenceHub victim, DamageHandlerBase dhb)
	{
		if (victim == base.Owner)
		{
			this.InitiateCombatTime();
		}
		else if (dhb is AttackerDamageHandler attackerDamageHandler && !(attackerDamageHandler.Attacker.Hub != base.Owner))
		{
			this.InitiateCombatTime();
		}
	}

	private void InitiateCombatTime()
	{
		this._regenCooldown = 0.7f;
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte(this._flockSize);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._flockSize = reader.ReadByte();
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (NetworkServer.active)
		{
			this._healthStat = base.Owner.playerStats.GetModule<HealthStat>();
			PlayerStats.OnAnyPlayerDamaged += OnPlayerDamaged;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._flockSize = 0;
		this.Multiplier = 0f;
		this._nearbyFlamingos.Clear();
		this._entireFlock.Clear();
		PlayerStats.OnAnyPlayerDamaged -= OnPlayerDamaged;
	}
}
