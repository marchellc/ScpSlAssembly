using System;
using System.Runtime.InteropServices;
using GameCore;
using Mirror;
using UnityEngine;

public class ServerTime : NetworkBehaviour
{
	public static bool CheckSynchronization(int myTime)
	{
		int num = Mathf.Abs(myTime - ServerTime.time);
		if (num > 2)
		{
			global::GameCore.Console.AddLog("Damage sync error.", new Color32(byte.MaxValue, 200, 0, byte.MaxValue), false, global::GameCore.Console.ConsoleLogType.Log);
		}
		return num <= 2;
	}

	private void Update()
	{
		this._rateLimit = false;
		if (this.timeFromStartup != 0)
		{
			ServerTime.time = this.timeFromStartup;
		}
	}

	private void Start()
	{
		if (base.isLocalPlayer && NetworkServer.active)
		{
			base.InvokeRepeating("IncreaseTime", 1f, 1f);
		}
	}

	private void IncreaseTime()
	{
		this.NetworktimeFromStartup = this.timeFromStartup + 1;
	}

	public override bool Weaved()
	{
		return true;
	}

	public int NetworktimeFromStartup
	{
		get
		{
			return this.timeFromStartup;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<int>(value, ref this.timeFromStartup, 1UL, null);
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this.timeFromStartup);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this.timeFromStartup);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<int>(ref this.timeFromStartup, null, reader.ReadInt());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<int>(ref this.timeFromStartup, null, reader.ReadInt());
		}
	}

	[SyncVar]
	public int timeFromStartup;

	public static int time;

	private const int AllowedDeviation = 2;

	private bool _rateLimit;
}
