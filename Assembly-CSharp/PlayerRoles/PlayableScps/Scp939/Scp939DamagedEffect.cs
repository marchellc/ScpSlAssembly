using System;
using System.Diagnostics;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939DamagedEffect : StandardSubroutine<Scp939Role>
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			if (!NetworkServer.active)
			{
				return;
			}
			this._lastTriggered.Restart();
			this._hpStat = base.Owner.playerStats.GetModule<HealthStat>();
			this._hume = base.CastRole.HumeShieldModule as DynamicHumeShieldController;
			this._eventAssigned = true;
			base.Owner.playerStats.OnThisPlayerDamaged += this.OnDamaged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			if (!this._eventAssigned)
			{
				return;
			}
			this._eventAssigned = false;
			base.Owner.playerStats.OnThisPlayerDamaged -= this.OnDamaged;
		}

		private void Update()
		{
			if (this._totalDamageReceived <= 0f)
			{
				return;
			}
			this._totalDamageReceived = Mathf.Clamp(this._totalDamageReceived - Time.deltaTime * 80f, 0f, 90f);
		}

		private void OnDamaged(DamageHandlerBase dhb)
		{
			AttackerDamageHandler attackerDamageHandler = dhb as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			if (this._hpStat.CurValue <= 0f)
			{
				return;
			}
			this._totalDamageReceived += attackerDamageHandler.AbsorbedHumeDamage + attackerDamageHandler.DealtHealthDamage;
			if (this._lastTriggered.Elapsed.TotalSeconds < 3.0)
			{
				return;
			}
			if (!this.CheckDamagedConditions(attackerDamageHandler))
			{
				return;
			}
			base.ServerSendRpc(true);
			this._lastTriggered.Restart();
		}

		private bool CheckDamagedConditions(AttackerDamageHandler adh)
		{
			float num = this._hpStat.CurValue + adh.DealtHealthDamage;
			float num2 = this._hume.HsCurrent + adh.AbsorbedHumeDamage;
			float num3 = this._hume.ShieldOverHealth.Evaluate(num);
			return Mathf.Approximately(num2, num3) || (adh.AbsorbedHumeDamage > 0f && this._hume.HsCurrent == 0f) || (this._lastTriggered.Elapsed.TotalSeconds > 10.0 && this._totalDamageReceived >= 90f);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)global::UnityEngine.Random.Range(0, 255));
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			Scp939Model scp939Model = base.CastRole.FpcModule.CharacterModelInstance as Scp939Model;
			if (scp939Model == null)
			{
				return;
			}
			scp939Model.PlayDamagedEffect((int)reader.ReadByte());
		}

		private bool _eventAssigned;

		private HealthStat _hpStat;

		private DynamicHumeShieldController _hume;

		private readonly Stopwatch _lastTriggered = new Stopwatch();

		private float _totalDamageReceived;

		private const float AbsoluteCooldown = 3f;

		private const float HighDamageCooldown = 10f;

		private const float HighDamageThreshold = 90f;

		private const float HighDamageDecay = 80f;
	}
}
