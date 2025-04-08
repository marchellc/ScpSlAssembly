using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Utils.Networking;

namespace PlayerStatsSystem
{
	public abstract class SyncedStatBase : StatBase
	{
		public event SyncedStatBase.StatChange OnStatChange;

		public SyncedStatBase()
		{
			this._canReceive = new Func<ReferenceHub, bool>(this.CanReceive);
		}

		public override float CurValue
		{
			get
			{
				return this._lastValue;
			}
			set
			{
				float lastValue = this._lastValue;
				this._lastValue = value;
				if (this.CheckDirty(this._lastSent, value))
				{
					this.ValueDirty = true;
				}
				if (lastValue == value)
				{
					return;
				}
				if (value != this.MaxValue)
				{
					SyncedStatBase.StatChange onStatChange = this.OnStatChange;
					if (onStatChange != null)
					{
						onStatChange(lastValue, value);
					}
				}
				this.OnValueChanged(lastValue, value);
			}
		}

		public byte SyncId
		{
			get
			{
				if (this._syncId != null)
				{
					return this._syncId.Value;
				}
				StatBase[] statModules = base.Hub.playerStats.StatModules;
				byte b = 0;
				for (int i = 0; i < statModules.Length; i++)
				{
					SyncedStatBase syncedStatBase = statModules[i] as SyncedStatBase;
					if (syncedStatBase != null)
					{
						SyncedStatBase syncedStatBase2 = syncedStatBase;
						byte b2 = b;
						b = b2 + 1;
						syncedStatBase2._syncId = new byte?(b2);
					}
				}
				return this._syncId.Value;
			}
		}

		public abstract SyncedStatBase.SyncMode Mode { get; }

		public abstract float ReadValue(NetworkReader reader);

		public abstract void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer);

		public abstract bool CheckDirty(float prevValue, float newValue);

		protected virtual void OnValueChanged(float prevValue, float newValue)
		{
		}

		public static SyncedStatBase GetStatOfUser(uint netId, byte syncId)
		{
			Dictionary<byte, SyncedStatBase> dictionary;
			if (!SyncedStatBase.AllSyncedStats.TryGetValue(netId, out dictionary))
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHubNetID(netId, out referenceHub))
				{
					throw new InvalidOperationException(string.Format("Cannot generate stats for non-existing user of NetId={0}", netId));
				}
				dictionary = new Dictionary<byte, SyncedStatBase>();
				StatBase[] statModules = referenceHub.playerStats.StatModules;
				for (int i = 0; i < statModules.Length; i++)
				{
					SyncedStatBase syncedStatBase = statModules[i] as SyncedStatBase;
					if (syncedStatBase != null)
					{
						dictionary.Add(syncedStatBase.SyncId, syncedStatBase);
					}
				}
				SyncedStatBase.AllSyncedStats[netId] = dictionary;
			}
			SyncedStatBase syncedStatBase2;
			if (!dictionary.TryGetValue(syncId, out syncedStatBase2))
			{
				throw new InvalidOperationException(string.Format("Stat of SyncId={0} does not exist.", syncId));
			}
			return syncedStatBase2;
		}

		internal override void Update()
		{
			base.Update();
			if (!NetworkServer.active)
			{
				return;
			}
			if (this.ValueDirty)
			{
				new SyncedStatMessages.StatMessage
				{
					Stat = this,
					Type = SyncedStatMessages.StatMessageType.CurrentValue,
					SyncedValue = this.CurValue
				}.SendToHubsConditionally(this._canReceive, 0);
				this.ValueDirty = false;
				this._lastSent = this.CurValue;
			}
			if (this.MaxValueDirty)
			{
				new SyncedStatMessages.StatMessage
				{
					Stat = this,
					Type = SyncedStatMessages.StatMessageType.MaxValue,
					SyncedValue = this.MaxValue
				}.SendToHubsConditionally(this._canReceive, 0);
				this.MaxValueDirty = false;
			}
		}

		internal override void ClassChanged()
		{
			base.ClassChanged();
			if (NetworkServer.active)
			{
				this.ValueDirty = true;
				this.MaxValueDirty = true;
			}
		}

		private bool CanReceive(ReferenceHub hub)
		{
			if (hub.isLocalPlayer)
			{
				return false;
			}
			bool flag;
			switch (this.Mode)
			{
			case SyncedStatBase.SyncMode.Private:
				flag = hub == base.Hub;
				break;
			case SyncedStatBase.SyncMode.PrivateAndSpectators:
				flag = !hub.IsAlive() || hub == base.Hub;
				break;
			case SyncedStatBase.SyncMode.Public:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			return flag;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void InitOnLoad()
		{
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub x)
			{
				SyncedStatBase.AllSyncedStats.Remove(x.netId);
			}));
		}

		private static readonly Dictionary<uint, Dictionary<byte, SyncedStatBase>> AllSyncedStats = new Dictionary<uint, Dictionary<byte, SyncedStatBase>>();

		protected bool ValueDirty;

		protected bool MaxValueDirty;

		private float _lastValue;

		private float _lastSent;

		private byte? _syncId;

		private readonly Func<ReferenceHub, bool> _canReceive;

		public delegate void StatChange(float oldValue, float newValue);

		public enum SyncMode
		{
			Private,
			PrivateAndSpectators,
			Public
		}
	}
}
