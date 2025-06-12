using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Utils.Networking;

namespace PlayerStatsSystem;

public abstract class SyncedStatBase : StatBase
{
	public delegate void StatChange(float oldValue, float newValue);

	public enum SyncMode
	{
		Private,
		PrivateAndSpectators,
		Public
	}

	private static readonly Dictionary<uint, Dictionary<byte, SyncedStatBase>> AllSyncedStats = new Dictionary<uint, Dictionary<byte, SyncedStatBase>>();

	protected bool ValueDirty;

	protected bool MaxValueDirty;

	private float _lastValue;

	private float _lastSent;

	private byte? _syncId;

	private readonly Func<ReferenceHub, bool> _canReceive;

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
			if (lastValue != value)
			{
				if (value != this.MaxValue)
				{
					this.OnStatChange?.Invoke(lastValue, value);
				}
				this.OnValueChanged(lastValue, value);
			}
		}
	}

	public byte SyncId
	{
		get
		{
			if (this._syncId.HasValue)
			{
				return this._syncId.Value;
			}
			StatBase[] statModules = base.Hub.playerStats.StatModules;
			byte b = 0;
			for (int i = 0; i < statModules.Length; i++)
			{
				if (statModules[i] is SyncedStatBase syncedStatBase)
				{
					syncedStatBase._syncId = b++;
				}
			}
			return this._syncId.Value;
		}
	}

	public abstract SyncMode Mode { get; }

	public event StatChange OnStatChange;

	public SyncedStatBase()
	{
		this._canReceive = CanReceive;
	}

	public abstract float ReadValue(SyncedStatMessages.StatMessageType type, NetworkReader reader);

	public abstract void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer);

	public abstract bool CheckDirty(float prevValue, float newValue);

	protected virtual void OnValueChanged(float prevValue, float newValue)
	{
	}

	public static SyncedStatBase GetStatOfUser(uint netId, byte syncId)
	{
		if (!SyncedStatBase.AllSyncedStats.TryGetValue(netId, out var value))
		{
			if (!ReferenceHub.TryGetHubNetID(netId, out var hub))
			{
				throw new InvalidOperationException($"Cannot generate stats for non-existing user of NetId={netId}");
			}
			value = new Dictionary<byte, SyncedStatBase>();
			StatBase[] statModules = hub.playerStats.StatModules;
			for (int i = 0; i < statModules.Length; i++)
			{
				if (statModules[i] is SyncedStatBase syncedStatBase)
				{
					value.Add(syncedStatBase.SyncId, syncedStatBase);
				}
			}
			SyncedStatBase.AllSyncedStats[netId] = value;
		}
		if (!value.TryGetValue(syncId, out var value2))
		{
			throw new InvalidOperationException($"Stat of SyncId={syncId} does not exist.");
		}
		return value2;
	}

	internal override void Update()
	{
		base.Update();
		if (NetworkServer.active)
		{
			if (this.ValueDirty)
			{
				new SyncedStatMessages.StatMessage
				{
					Stat = this,
					Type = SyncedStatMessages.StatMessageType.CurrentValue,
					SyncedValue = this.CurValue
				}.SendToHubsConditionally(this._canReceive);
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
				}.SendToHubsConditionally(this._canReceive);
				this.MaxValueDirty = false;
			}
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
		return this.Mode switch
		{
			SyncMode.Private => hub == base.Hub, 
			SyncMode.PrivateAndSpectators => !hub.IsAlive() || hub == base.Hub, 
			SyncMode.Public => true, 
			_ => false, 
		};
	}

	[RuntimeInitializeOnLoadMethod]
	private static void InitOnLoad()
	{
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub x)
		{
			SyncedStatBase.AllSyncedStats.Remove(x.netId);
		};
	}
}
