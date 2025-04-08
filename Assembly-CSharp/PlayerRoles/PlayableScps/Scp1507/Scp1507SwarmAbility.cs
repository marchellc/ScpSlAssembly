using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507SwarmAbility : StandardSubroutine<Scp1507Role>
	{
		public int FlockSize
		{
			get
			{
				return (int)this._flockSize;
			}
		}

		public float Multiplier { get; private set; }

		private void UpdateMultiplier()
		{
			this.Multiplier = Mathf.MoveTowards(this.Multiplier, (float)this.FlockSize, 0.6f * Time.deltaTime);
		}

		private void Update()
		{
			this.UpdateMultiplier();
			if (!NetworkServer.active)
			{
				return;
			}
			this.ServerUpdateNearby();
			this.ServerUpdateFlock();
			this.ServerUpdateHealthRegen();
		}

		private void ServerUpdateNearby()
		{
			Vector3 position = base.CastRole.FpcModule.Position;
			this._nearbyFlamingos.Clear();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp1507Role scp1507Role = referenceHub.roleManager.CurrentRole as Scp1507Role;
				if (scp1507Role != null && scp1507Role.Team == Team.Flamingos)
				{
					Vector3 position2 = scp1507Role.FpcModule.Position;
					if ((position - position2).sqrMagnitude <= 56.25f)
					{
						this._nearbyFlamingos.Add(scp1507Role);
					}
				}
			}
		}

		private void ServerUpdateFlock()
		{
			this._entireFlock.Clear();
			foreach (Scp1507Role scp1507Role in this._nearbyFlamingos)
			{
				this._entireFlock.Add(scp1507Role);
				Scp1507SwarmAbility scp1507SwarmAbility;
				if (scp1507Role.SubroutineModule.TryGetSubroutine<Scp1507SwarmAbility>(out scp1507SwarmAbility))
				{
					foreach (Scp1507Role scp1507Role2 in scp1507SwarmAbility._nearbyFlamingos)
					{
						this._entireFlock.Add(scp1507Role2);
					}
				}
			}
			int num = Mathf.Max(0, this._entireFlock.Count - 1);
			if (num == (int)this._flockSize)
			{
				return;
			}
			this._flockSize = (byte)num;
			base.ServerSendRpc(true);
		}

		private void ServerUpdateHealthRegen()
		{
			if (this._regenCooldown >= 0.7f)
			{
				this._regenCooldown -= Time.deltaTime;
				return;
			}
			this._healthStat.ServerHeal(this.Multiplier * 1f * Time.deltaTime);
		}

		private void OnPlayerDamaged(ReferenceHub victim, DamageHandlerBase dhb)
		{
			if (victim == base.Owner)
			{
				this.InitiateCombatTime();
				return;
			}
			AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			if (attackerDamageHandler.Attacker.Hub != base.Owner)
			{
				return;
			}
			this.InitiateCombatTime();
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
			if (!NetworkServer.active)
			{
				return;
			}
			this._healthStat = base.Owner.playerStats.GetModule<HealthStat>();
			PlayerStats.OnAnyPlayerDamaged += this.OnPlayerDamaged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._flockSize = 0;
			this.Multiplier = 0f;
			this._nearbyFlamingos.Clear();
			this._entireFlock.Clear();
			PlayerStats.OnAnyPlayerDamaged -= this.OnPlayerDamaged;
		}

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
	}
}
