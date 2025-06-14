using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore;

public class RoundStart : NetworkBehaviour
{
	public static RoundStart singleton;

	public static bool LobbyLock;

	private static bool _singletonSet;

	[SyncVar]
	public short Timer = -2;

	private short _lastTimer = -1;

	internal static readonly Stopwatch RoundStartTimer;

	public static bool RoundStarted
	{
		get
		{
			if (RoundStart._singletonSet)
			{
				return RoundStart.singleton.Timer == -1;
			}
			return false;
		}
	}

	public static TimeSpan RoundLength => RoundStart.RoundStartTimer.Elapsed;

	public short NetworkTimer
	{
		get
		{
			return this.Timer;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Timer, 1uL, null);
		}
	}

	static RoundStart()
	{
		RoundStart.RoundStartTimer = new Stopwatch();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		RoundStart.RoundStartTimer.Reset();
	}

	private void Start()
	{
		base.GetComponent<RectTransform>().localPosition = Vector3.zero;
	}

	private void Awake()
	{
		RoundStart.singleton = this;
		RoundStart._singletonSet = true;
	}

	private void OnDestroy()
	{
		RoundStart._singletonSet = false;
	}

	private void Update()
	{
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
			writer.WriteShort(this.Timer);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteShort(this.Timer);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Timer, null, reader.ReadShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Timer, null, reader.ReadShort());
		}
	}
}
