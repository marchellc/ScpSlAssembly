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
			if (_singletonSet)
			{
				return singleton.Timer == -1;
			}
			return false;
		}
	}

	public static TimeSpan RoundLength => RoundStartTimer.Elapsed;

	public short NetworkTimer
	{
		get
		{
			return Timer;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref Timer, 1uL, null);
		}
	}

	static RoundStart()
	{
		RoundStartTimer = new Stopwatch();
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		RoundStartTimer.Reset();
	}

	private void Start()
	{
		GetComponent<RectTransform>().localPosition = Vector3.zero;
	}

	private void Awake()
	{
		singleton = this;
		_singletonSet = true;
	}

	private void OnDestroy()
	{
		_singletonSet = false;
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
			writer.WriteShort(Timer);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteShort(Timer);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref Timer, null, reader.ReadShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref Timer, null, reader.ReadShort());
		}
	}
}
