using System;
using System.Collections.Generic;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class AhpStat : SyncedStatBase
	{
		public override SyncedStatBase.SyncMode Mode
		{
			get
			{
				return SyncedStatBase.SyncMode.PrivateAndSpectators;
			}
		}

		public override float MinValue
		{
			get
			{
				return 0f;
			}
		}

		public override float MaxValue
		{
			get
			{
				return this._maxSoFar;
			}
			set
			{
				this._maxSoFar = value;
				this.MaxValueDirty = true;
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

		public override float ReadValue(NetworkReader reader)
		{
			return (float)reader.ReadUShort();
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

		public AhpStat.AhpProcess ServerAddProcess(float amount, float limit, float decay, float efficacy, float sustain, bool persistant)
		{
			float num = 0f;
			float num2 = limit;
			foreach (AhpStat.AhpProcess ahpProcess in this._activeProcesses)
			{
				num += ahpProcess.CurrentAmount;
				num2 = Mathf.Max(num2, ahpProcess.Limit);
			}
			float num3 = num + amount - num2;
			if (num3 > 0f)
			{
				amount = Mathf.Max(0f, amount - num3);
			}
			AhpStat.AhpProcess ahpProcess2 = new AhpStat.AhpProcess(amount, limit, decay, efficacy, sustain, persistant);
			for (int i = 0; i < this._activeProcesses.Count; i++)
			{
				if (efficacy >= this._activeProcesses[i].Efficacy)
				{
					this._activeProcesses.Insert(i, ahpProcess2);
					return ahpProcess2;
				}
			}
			this._activeProcesses.Add(ahpProcess2);
			return ahpProcess2;
		}

		public AhpStat.AhpProcess ServerAddProcess(float amount)
		{
			return this.ServerAddProcess(amount, this.MaxValue, 1.2f, 0.7f, 0f, false);
		}

		public bool ServerTryGetProcess(int killcode, out AhpStat.AhpProcess process)
		{
			foreach (AhpStat.AhpProcess ahpProcess in this._activeProcesses)
			{
				if (ahpProcess.KillCode == killcode)
				{
					process = ahpProcess;
					return true;
				}
			}
			process = null;
			return false;
		}

		public bool ServerKillProcess(int killcode)
		{
			AhpStat.AhpProcess ahpProcess;
			return this.ServerTryGetProcess(killcode, out ahpProcess) && this._activeProcesses.Remove(ahpProcess);
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
			foreach (AhpStat.AhpProcess ahpProcess in this._activeProcesses)
			{
				float num = damage * ahpProcess.Efficacy;
				if (num < ahpProcess.CurrentAmount)
				{
					ahpProcess.CurrentAmount -= num;
					return damage - num;
				}
				damage -= ahpProcess.CurrentAmount;
				ahpProcess.CurrentAmount = 0f;
			}
			return damage;
		}

		private void ServerUpdateProcesses()
		{
			float num = 0f;
			List<int> list = ListPool<int>.Shared.Rent();
			for (int i = 0; i < this._activeProcesses.Count; i++)
			{
				AhpStat.AhpProcess ahpProcess = this._activeProcesses[i];
				num += ahpProcess.CurrentAmount;
				if (ahpProcess.SustainTime > 0f)
				{
					ahpProcess.SustainTime -= Time.deltaTime;
				}
				else
				{
					ahpProcess.CurrentAmount = Mathf.Clamp(ahpProcess.CurrentAmount - ahpProcess.DecayRate * Time.deltaTime, 0f, ahpProcess.Limit);
					if (ahpProcess.CurrentAmount == 0f && !ahpProcess.Persistant)
					{
						list.Add(i - list.Count);
					}
				}
			}
			foreach (int num2 in list)
			{
				this._activeProcesses.RemoveAt(num2);
			}
			ListPool<int>.Shared.Return(list);
			this.CurValue = Mathf.Clamp(num, this.MinValue, this.MaxValue);
		}

		public const float DefaultMax = 75f;

		public const float DefaultEfficacy = 0.7f;

		public const float DefaultDecay = 1.2f;

		private readonly List<AhpStat.AhpProcess> _activeProcesses = new List<AhpStat.AhpProcess>();

		private float _maxSoFar;

		public class AhpProcess
		{
			public AhpProcess(float startAmount, float limit, float decay, float efficacy, float sustain, bool persistant)
			{
				AhpStat.AhpProcess._killCodeAI++;
				this.CurrentAmount = startAmount;
				this.Limit = limit;
				this.DecayRate = decay;
				this.Efficacy = efficacy;
				this.SustainTime = sustain;
				this.Persistant = persistant;
				this.KillCode = AhpStat.AhpProcess._killCodeAI;
			}

			public float CurrentAmount;

			public float Limit;

			public float DecayRate;

			public float Efficacy;

			public float SustainTime;

			public readonly bool Persistant;

			public readonly int KillCode;

			private static int _killCodeAI;
		}
	}
}
