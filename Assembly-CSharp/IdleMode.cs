using System;
using System.Diagnostics;
using Mirror;
using ServerOutput;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleMode : MonoBehaviour
{
	public static bool IdleModeActive { get; private set; }

	public static bool PauseIdleMode
	{
		get
		{
			return IdleMode._pauseIdleMode;
		}
		set
		{
			if (IdleMode._pauseIdleMode == value)
			{
				return;
			}
			IdleMode._pauseIdleMode = value;
			if (value)
			{
				IdleMode.SetIdleMode(false);
				IdleMode._st.Reset();
				IdleMode.PreauthStopwatch.Reset();
				if (!IdleMode._idleModeEnabled)
				{
					return;
				}
				ServerConsole.AddLog("Idle mode is now temporarily blocked.", ConsoleColor.Gray, false);
				return;
			}
			else
			{
				if (!IdleMode._idleModeEnabled)
				{
					return;
				}
				ServerConsole.AddLog("Idle mode is now available.", ConsoleColor.Gray, false);
				IdleMode._st.Restart();
				IdleMode.PreauthStopwatch.Restart();
				return;
			}
		}
	}

	public static bool IdleModeEnabled
	{
		get
		{
			return IdleMode._idleModeEnabled;
		}
		set
		{
			IdleMode._idleModeEnabled = value;
			if (!IdleMode._idleModeEnabled && IdleMode.IdleModeActive)
			{
				IdleMode.SetIdleMode(false);
			}
		}
	}

	internal static short IdleModeTickrate
	{
		get
		{
			return IdleMode._idleModeTickrate;
		}
		set
		{
			IdleMode._idleModeTickrate = ((value < -1 || value == 0) ? 1 : value);
			if (IdleMode.IdleModeActive)
			{
				IdleMode.SetIdleMode(true, true);
			}
		}
	}

	private void Start()
	{
		SceneManager.sceneLoaded += IdleMode.OnSceneLoaded;
		ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(delegate(ReferenceHub rh)
		{
			IdleMode.SetIdleMode(false);
		}));
		ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub rh)
		{
			if (ReferenceHub.AllHubs.Count <= 1)
			{
				IdleMode.SetIdleMode(true);
			}
		}));
	}

	private void FixedUpdate()
	{
		if (IdleMode._st.ElapsedMilliseconds < (long)((ulong)IdleMode.IdleModeTime) || IdleMode._pauseIdleMode || (IdleMode.PreauthStopwatch.IsRunning && IdleMode.PreauthStopwatch.ElapsedMilliseconds < (long)((ulong)IdleMode.IdleModePreauthTime)))
		{
			return;
		}
		IdleMode._st.Reset();
		IdleMode.PreauthStopwatch.Reset();
		if (ReferenceHub.AllHubs.Count <= 1)
		{
			IdleMode.SetIdleMode(true);
		}
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		IdleMode._st.Reset();
		if (ServerStatic.IsDedicated && !IdleMode._pauseIdleMode && IdleMode.IdleModeEnabled && scene.name == "Facility")
		{
			IdleMode._st.Start();
		}
	}

	public static void SetIdleMode(bool state)
	{
		IdleMode.SetIdleMode(state, false);
	}

	private static void SetIdleMode(bool state, bool force)
	{
		if (!NetworkServer.active || (state && IdleMode._pauseIdleMode && !force) || (state == IdleMode.IdleModeActive && !force) || (state && !IdleMode.IdleModeEnabled) || !ServerStatic.IsDedicated)
		{
			return;
		}
		if (state)
		{
			Application.targetFrameRate = (int)IdleMode.IdleModeTickrate;
			Time.timeScale = 0.01f;
			ServerConsole.AddLog("Server has entered the idle mode.", ConsoleColor.Gray, false);
			ServerConsole.AddOutputEntry(default(IdleEnterEntry));
		}
		else
		{
			Application.targetFrameRate = (int)ServerStatic.ServerTickrate;
			Time.timeScale = 1f;
			ServerConsole.AddLog("Server has exited the idle mode.", ConsoleColor.Gray, false);
			ServerConsole.AddOutputEntry(default(IdleExitEntry));
		}
		IdleMode.IdleModeActive = state;
	}

	public static uint IdleModeTime;

	public static uint IdleModePreauthTime;

	private static bool _idleModeEnabled = true;

	private static bool _pauseIdleMode = true;

	private static short _idleModeTickrate = 1;

	private static readonly Stopwatch _st = new Stopwatch();

	internal static readonly Stopwatch PreauthStopwatch = new Stopwatch();
}
