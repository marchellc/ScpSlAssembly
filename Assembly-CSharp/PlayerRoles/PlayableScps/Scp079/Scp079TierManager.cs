using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079TierManager : StandardSubroutine<Scp079Role>
{
	private readonly struct ExpQueuedNotification
	{
		public readonly int ExpAmount;

		public readonly Scp079HudTranslation Reason;

		public readonly RoleTypeId Subject;

		public void Write(NetworkWriter writer)
		{
			writer.WriteUShort((ushort)this.ExpAmount);
			writer.WriteByte((byte)this.Reason);
			writer.WriteRoleType(this.Subject);
		}

		public ExpQueuedNotification(NetworkReader reader)
		{
			this.ExpAmount = reader.ReadUShort();
			this.Reason = (Scp079HudTranslation)reader.ReadByte();
			this.Subject = reader.ReadRoleType();
		}

		public ExpQueuedNotification(int amount, Scp079HudTranslation reason, RoleTypeId subject)
		{
			this.ExpAmount = Mathf.Clamp(amount, 0, 65535);
			this.Reason = reason;
			this.Subject = subject;
		}
	}

	private readonly Queue<ExpQueuedNotification> _expGainQueue = new Queue<ExpQueuedNotification>();

	private int _totalExp;

	private bool _valueDirty;

	private int _accessTier;

	private int _thresholdsCount;

	[SerializeField]
	private int[] _levelupThresholds;

	public Action OnLevelledUp;

	public Action OnTierChanged;

	public Action OnExpChanged;

	public int[] AbsoluteThresholds { get; private set; }

	public int TotalExp
	{
		get
		{
			return this._totalExp;
		}
		set
		{
			this._totalExp = value;
			this.OnExpChanged?.Invoke();
			int num = 0;
			for (int i = 0; i < this._thresholdsCount && this._totalExp >= this.AbsoluteThresholds[i]; i++)
			{
				num++;
			}
			this.AccessTierIndex = num;
			if (NetworkServer.active)
			{
				this._valueDirty = true;
			}
		}
	}

	public int RelativeExp
	{
		get
		{
			int num = this.AccessTierIndex - 1;
			if (num < 0)
			{
				return Mathf.FloorToInt(this.TotalExp);
			}
			float f = this.TotalExp - this.AbsoluteThresholds[num];
			return Mathf.Min(this.NextLevelThreshold, Mathf.FloorToInt(f));
		}
	}

	public int NextLevelThreshold
	{
		get
		{
			if (this.AccessTierIndex >= this._thresholdsCount)
			{
				return -1;
			}
			return this._levelupThresholds[this.AccessTierIndex];
		}
	}

	public int AccessTierIndex
	{
		get
		{
			return Mathf.Clamp(this._accessTier, 0, this._thresholdsCount);
		}
		private set
		{
			if (this._accessTier != value)
			{
				int num = value - this._accessTier;
				for (int i = 0; i < num; i++)
				{
					this._accessTier++;
					this.OnLevelledUp?.Invoke();
				}
				Scp079LevelingUpEventArgs e = new Scp079LevelingUpEventArgs(base.Owner, value + 1);
				Scp079Events.OnLevelingUp(e);
				if (e.IsAllowed)
				{
					this._accessTier = value;
					this.OnTierChanged?.Invoke();
					Scp079Events.OnLeveledUp(new Scp079LeveledUpEventArgs(base.Owner, value + 1));
				}
			}
		}
	}

	public int AccessTierLevel => this.AccessTierIndex + 1;

	private void Update()
	{
		if (NetworkServer.active && this._valueDirty)
		{
			base.ServerSendRpc(toAll: true);
			this._valueDirty = false;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		int num = 0;
		this._thresholdsCount = this._levelupThresholds.Length;
		this.AbsoluteThresholds = new int[this._thresholdsCount];
		for (int i = 0; i < this._thresholdsCount; i++)
		{
			num += this._levelupThresholds[i];
			this.AbsoluteThresholds[i] = num;
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		ReferenceHub.OnPlayerAdded += base.ServerSendRpc;
		this.TotalExp = 0;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		ReferenceHub.OnPlayerAdded -= base.ServerSendRpc;
	}

	public void ServerGrantExperience(int amount, Scp079HudTranslation reason, RoleTypeId subject = RoleTypeId.None)
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("SCP-079 experience cannot be granted by local player!");
		}
		if (amount > 0)
		{
			this._expGainQueue.Enqueue(new ExpQueuedNotification(amount, reason, subject));
			this.TotalExp += amount;
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteUShort((ushort)this.TotalExp);
		ExpQueuedNotification result;
		while (this._expGainQueue.TryDequeue(out result))
		{
			result.Write(writer);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		ushort totalExp = reader.ReadUShort();
		if (!Scp079Role.LocalInstanceActive && !base.CastRole.IsSpectated && !NetworkServer.active)
		{
			this.TotalExp = totalExp;
			return;
		}
		while (reader.Remaining > 0)
		{
			ExpQueuedNotification expQueuedNotification = new ExpQueuedNotification(reader);
			if (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(expQueuedNotification.Subject, out var result))
			{
				Scp079NotificationManager.AddNotification(expQueuedNotification.Reason, expQueuedNotification.ExpAmount, result.RoleName);
			}
		}
		if (!NetworkServer.active)
		{
			this.TotalExp = totalExp;
		}
	}
}
