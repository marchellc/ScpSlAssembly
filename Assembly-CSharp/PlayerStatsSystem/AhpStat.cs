using System.Collections.Generic;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerStatsSystem;

public class AhpStat : SyncedStatBase
{
	public class AhpProcess
	{
		public float CurrentAmount;

		public float Limit;

		public float DecayRate;

		public float Efficacy;

		public float SustainTime;

		public readonly bool Persistant;

		public readonly int KillCode;

		private static int _killCodeAI;

		public AhpProcess(float startAmount, float limit, float decay, float efficacy, float sustain, bool persistant)
		{
			AhpProcess._killCodeAI++;
			this.CurrentAmount = startAmount;
			this.Limit = limit;
			this.DecayRate = decay;
			this.Efficacy = efficacy;
			this.SustainTime = sustain;
			this.Persistant = persistant;
			this.KillCode = AhpProcess._killCodeAI;
		}
	}

	public const float DefaultMax = 75f;

	public const float DefaultEfficacy = 0.7f;

	public const float DefaultDecay = 1.2f;

	private readonly List<AhpProcess> _activeProcesses = new List<AhpProcess>();

	private float _maxSoFar;

	public override SyncMode Mode => SyncMode.PrivateAndSpectators;

	public override float MinValue => 0f;

	public override float MaxValue
	{
		get
		{
			return this._maxSoFar;
		}
		set
		{
			this._maxSoFar = value;
			base.MaxValueDirty = true;
		}
	}

	internal override void ClassChanged()
	{
		this._maxSoFar = 75f;
		if (NetworkServer.active)
		{
			this._activeProcesses.Clear();
		}
	}

	internal override void Update()
	{
		base.Update();
		if (NetworkServer.active)
		{
			this.ServerUpdateProcesses();
		}
		if (this.CurValue == this.MinValue)
		{
			this._maxSoFar = 75f;
		}
	}

	protected override void OnValueChanged(float prevValue, float newValue)
	{
		this._maxSoFar = Mathf.Max(this._maxSoFar, newValue);
	}

	public override float ReadValue(SyncedStatMessages.StatMessageType type, NetworkReader reader)
	{
		return (int)reader.ReadUShort();
	}

	public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
	{
		int num = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? Mathf.Clamp(Mathf.CeilToInt(this.CurValue), 0, 65535) : Mathf.Clamp(Mathf.CeilToInt(this.MaxValue), 0, 65535));
		writer.WriteUShort((ushort)num);
	}

	public override bool CheckDirty(float prevValue, float newValue)
	{
		return Mathf.CeilToInt(prevValue) != Mathf.CeilToInt(newValue);
	}

	public AhpProcess ServerAddProcess(float amount, float limit, float decay, float efficacy, float sustain, bool persistant)
	{
		float num = 0f;
		float num2 = limit;
		foreach (AhpProcess activeProcess in this._activeProcesses)
		{
			num += activeProcess.CurrentAmount;
			num2 = Mathf.Max(num2, activeProcess.Limit);
		}
		float num3 = num + amount - num2;
		if (num3 > 0f)
		{
			amount = Mathf.Max(0f, amount - num3);
		}
		AhpProcess ahpProcess = new AhpProcess(amount, limit, decay, efficacy, sustain, persistant);
		for (int i = 0; i < this._activeProcesses.Count; i++)
		{
			if (!(efficacy < this._activeProcesses[i].Efficacy))
			{
				this._activeProcesses.Insert(i, ahpProcess);
				return ahpProcess;
			}
		}
		this._activeProcesses.Add(ahpProcess);
		return ahpProcess;
	}

	public AhpProcess ServerAddProcess(float amount)
	{
		return this.ServerAddProcess(amount, this.MaxValue, 1.2f, 0.7f, 0f, persistant: false);
	}

	public bool ServerTryGetProcess(int killcode, out AhpProcess process)
	{
		foreach (AhpProcess activeProcess in this._activeProcesses)
		{
			if (activeProcess.KillCode == killcode)
			{
				process = activeProcess;
				return true;
			}
		}
		process = null;
		return false;
	}

	public bool ServerKillProcess(int killcode)
	{
		if (this.ServerTryGetProcess(killcode, out var process))
		{
			return this._activeProcesses.Remove(process);
		}
		return false;
	}

	public void ServerKillAllProcesses()
	{
		this._activeProcesses.Clear();
	}

	public float ServerProcessDamage(float damage)
	{
		if (damage <= 0f)
		{
			return damage;
		}
		foreach (AhpProcess activeProcess in this._activeProcesses)
		{
			float num = damage * activeProcess.Efficacy;
			if (num >= activeProcess.CurrentAmount)
			{
				damage -= activeProcess.CurrentAmount;
				activeProcess.CurrentAmount = 0f;
				continue;
			}
			activeProcess.CurrentAmount -= num;
			return damage - num;
		}
		return damage;
	}

	private void ServerUpdateProcesses()
	{
		float num = 0f;
		List<int> list = ListPool<int>.Shared.Rent();
		for (int i = 0; i < this._activeProcesses.Count; i++)
		{
			AhpProcess ahpProcess = this._activeProcesses[i];
			num += ahpProcess.CurrentAmount;
			if (ahpProcess.SustainTime > 0f)
			{
				ahpProcess.SustainTime -= Time.deltaTime;
				continue;
			}
			ahpProcess.CurrentAmount = Mathf.Clamp(ahpProcess.CurrentAmount - ahpProcess.DecayRate * Time.deltaTime, 0f, ahpProcess.Limit);
			if (ahpProcess.CurrentAmount == 0f && !ahpProcess.Persistant)
			{
				list.Add(i - list.Count);
			}
		}
		foreach (int item in list)
		{
			this._activeProcesses.RemoveAt(item);
		}
		ListPool<int>.Shared.Return(list);
		this.CurValue = Mathf.Clamp(num, this.MinValue, this.MaxValue);
	}
}
