using System.Diagnostics;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939DamagedEffect : StandardSubroutine<Scp939Role>
{
	private bool _eventAssigned;

	private HealthStat _hpStat;

	private DynamicHumeShieldController _hume;

	private readonly Stopwatch _lastTriggered = new Stopwatch();

	private float _totalDamageReceived;

	private const float AbsoluteCooldown = 3f;

	private const float HighDamageCooldown = 10f;

	private const float HighDamageThreshold = 90f;

	private const float HighDamageDecay = 80f;

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (NetworkServer.active)
		{
			this._lastTriggered.Restart();
			this._hpStat = base.Owner.playerStats.GetModule<HealthStat>();
			this._hume = base.CastRole.HumeShieldModule as DynamicHumeShieldController;
			this._eventAssigned = true;
			base.Owner.playerStats.OnThisPlayerDamaged += OnDamaged;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		if (this._eventAssigned)
		{
			this._eventAssigned = false;
			base.Owner.playerStats.OnThisPlayerDamaged -= OnDamaged;
		}
	}

	private void Update()
	{
		if (!(this._totalDamageReceived <= 0f))
		{
			this._totalDamageReceived = Mathf.Clamp(this._totalDamageReceived - Time.deltaTime * 80f, 0f, 90f);
		}
	}

	private void OnDamaged(DamageHandlerBase dhb)
	{
		if (dhb is AttackerDamageHandler attackerDamageHandler && !(this._hpStat.CurValue <= 0f))
		{
			this._totalDamageReceived += attackerDamageHandler.TotalDamageDealt;
			if (!(this._lastTriggered.Elapsed.TotalSeconds < 3.0) && this.CheckDamagedConditions(attackerDamageHandler))
			{
				base.ServerSendRpc(toAll: true);
				this._lastTriggered.Restart();
			}
		}
	}

	private bool CheckDamagedConditions(AttackerDamageHandler adh)
	{
		float time = this._hpStat.CurValue + adh.DealtHealthDamage;
		float a = this._hume.HsCurrent + adh.AbsorbedHumeDamage;
		float b = this._hume.ShieldOverHealth.Evaluate(time);
		if (Mathf.Approximately(a, b))
		{
			return true;
		}
		if (adh.AbsorbedHumeDamage > 0f && this._hume.HsCurrent == 0f)
		{
			return true;
		}
		if (this._lastTriggered.Elapsed.TotalSeconds > 10.0 && this._totalDamageReceived >= 90f)
		{
			return true;
		}
		return false;
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)Random.Range(0, 255));
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (base.CastRole.FpcModule.CharacterModelInstance is Scp939Model scp939Model)
		{
			scp939Model.PlayDamagedEffect(reader.ReadByte());
		}
	}
}
