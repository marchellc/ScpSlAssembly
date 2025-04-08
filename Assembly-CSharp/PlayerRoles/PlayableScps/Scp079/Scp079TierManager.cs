using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079TierManager : StandardSubroutine<Scp079Role>
	{
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
				Action onExpChanged = this.OnExpChanged;
				if (onExpChanged != null)
				{
					onExpChanged();
				}
				int num = 0;
				int num2 = 0;
				while (num2 < this._thresholdsCount && this._totalExp >= this.AbsoluteThresholds[num2])
				{
					num++;
					num2++;
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
					return Mathf.FloorToInt((float)this.TotalExp);
				}
				float num2 = (float)(this.TotalExp - this.AbsoluteThresholds[num]);
				return Mathf.Min(this.NextLevelThreshold, Mathf.FloorToInt(num2));
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
				if (this._accessTier == value)
				{
					return;
				}
				int num = value - this._accessTier;
				for (int i = 0; i < num; i++)
				{
					this._accessTier++;
					Action onLevelledUp = this.OnLevelledUp;
					if (onLevelledUp != null)
					{
						onLevelledUp();
					}
				}
				Scp079LevelingUpEventArgs scp079LevelingUpEventArgs = new Scp079LevelingUpEventArgs(base.Owner, value + 1);
				Scp079Events.OnLevelingUp(scp079LevelingUpEventArgs);
				if (!scp079LevelingUpEventArgs.IsAllowed)
				{
					return;
				}
				this._accessTier = value;
				Action onTierChanged = this.OnTierChanged;
				if (onTierChanged != null)
				{
					onTierChanged();
				}
				Scp079Events.OnLeveledUp(new Scp079LeveledUpEventArgs(base.Owner, value + 1));
			}
		}

		public int AccessTierLevel
		{
			get
			{
				return this.AccessTierIndex + 1;
			}
		}

		private void Update()
		{
			if (!NetworkServer.active || !this._valueDirty)
			{
				return;
			}
			base.ServerSendRpc(true);
			this._valueDirty = false;
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
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(base.ServerSendRpc));
			this.TotalExp = 0;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(base.ServerSendRpc));
		}

		public void ServerGrantExperience(int amount, Scp079HudTranslation reason, RoleTypeId subject = RoleTypeId.None)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("SCP-079 experience cannot be granted by local player!");
			}
			if (amount <= 0)
			{
				return;
			}
			this._expGainQueue.Enqueue(new Scp079TierManager.ExpQueuedNotification(amount, reason, subject));
			this.TotalExp += amount;
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteUShort((ushort)this.TotalExp);
			Scp079TierManager.ExpQueuedNotification expQueuedNotification;
			while (this._expGainQueue.TryDequeue(out expQueuedNotification))
			{
				expQueuedNotification.Write(writer);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			ushort num = reader.ReadUShort();
			if (!Scp079Role.LocalInstanceActive && !base.CastRole.IsSpectated && !NetworkServer.active)
			{
				this.TotalExp = (int)num;
				return;
			}
			while (reader.Remaining > 0)
			{
				Scp079TierManager.ExpQueuedNotification expQueuedNotification = new Scp079TierManager.ExpQueuedNotification(reader);
				PlayerRoleBase playerRoleBase;
				if (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(expQueuedNotification.Subject, out playerRoleBase))
				{
					Scp079NotificationManager.AddNotification(expQueuedNotification.Reason, new object[] { expQueuedNotification.ExpAmount, playerRoleBase.RoleName });
				}
			}
			if (NetworkServer.active)
			{
				return;
			}
			this.TotalExp = (int)num;
		}

		private readonly Queue<Scp079TierManager.ExpQueuedNotification> _expGainQueue = new Queue<Scp079TierManager.ExpQueuedNotification>();

		private int _totalExp;

		private bool _valueDirty;

		private int _accessTier;

		private int _thresholdsCount;

		[SerializeField]
		private int[] _levelupThresholds;

		public Action OnLevelledUp;

		public Action OnTierChanged;

		public Action OnExpChanged;

		private readonly struct ExpQueuedNotification
		{
			public void Write(NetworkWriter writer)
			{
				writer.WriteUShort((ushort)this.ExpAmount);
				writer.WriteByte((byte)this.Reason);
				writer.WriteRoleType(this.Subject);
			}

			public ExpQueuedNotification(NetworkReader reader)
			{
				this.ExpAmount = (int)reader.ReadUShort();
				this.Reason = (Scp079HudTranslation)reader.ReadByte();
				this.Subject = reader.ReadRoleType();
			}

			public ExpQueuedNotification(int amount, Scp079HudTranslation reason, RoleTypeId subject)
			{
				this.ExpAmount = Mathf.Clamp(amount, 0, 65535);
				this.Reason = reason;
				this.Subject = subject;
			}

			public readonly int ExpAmount;

			public readonly Scp079HudTranslation Reason;

			public readonly RoleTypeId Subject;
		}
	}
}
