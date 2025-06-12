using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace MapGeneration.Decoration;

public class EnvironmentalAlarmSync : NetworkBehaviour
{
	[HideInInspector]
	[SyncVar(hook = "HandleActivityChanged")]
	public bool IsEnabled;

	private EnvironmentalAlarm _targetAlarm;

	private float _timeLeft;

	public float TimeLeft
	{
		get
		{
			return this._timeLeft;
		}
		set
		{
			if (this._timeLeft != value)
			{
				this._timeLeft = Mathf.Max(value, 0f);
			}
		}
	}

	public bool NetworkIsEnabled
	{
		get
		{
			return this.IsEnabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.IsEnabled, 1uL, HandleActivityChanged);
		}
	}

	public void ServerTriggerAlarm(float duration)
	{
		this.TimeLeft += duration;
	}

	private void Start()
	{
		if (!base.TryGetComponent<EnvironmentalAlarm>(out this._targetAlarm))
		{
			throw new NullReferenceException();
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.ProcessActivity();
		}
	}

	private void ProcessActivity()
	{
		this.TimeLeft -= Time.deltaTime;
		this.NetworkIsEnabled = this.TimeLeft > 0f;
	}

	private void HandleActivityChanged(bool oldValue, bool newValue)
	{
		this._targetAlarm.IsEnabled = newValue;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.IsEnabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.IsEnabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsEnabled, HandleActivityChanged, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsEnabled, HandleActivityChanged, reader.ReadBool());
		}
	}
}
