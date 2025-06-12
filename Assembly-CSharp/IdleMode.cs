using System.Diagnostics;
using Mirror;
using ServerOutput;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleMode : MonoBehaviour
{
	public static uint IdleModeTime;

	public static uint IdleModePreauthTime;

	private static bool _idleModeEnabled = true;

	private static bool _pauseIdleMode = true;

	private static short _idleModeTickrate = 1;

	private static readonly Stopwatch _st = new Stopwatch();

	internal static readonly Stopwatch PreauthStopwatch = new Stopwatch();

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
				IdleMode.SetIdleMode(state: false);
				IdleMode._st.Reset();
				IdleMode.PreauthStopwatch.Reset();
				if (IdleMode._idleModeEnabled)
				{
					ServerConsole.AddLog("Idle mode is now temporarily blocked.");
				}
			}
			else if (IdleMode._idleModeEnabled)
			{
				ServerConsole.AddLog("Idle mode is now available.");
				IdleMode._st.Restart();
				IdleMode.PreauthStopwatch.Restart();
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
				IdleMode.SetIdleMode(state: false);
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
			IdleMode._idleModeTickrate = (short)((value < -1 || value == 0) ? 1 : value);
			if (IdleMode.IdleModeActive)
			{
				IdleMode.SetIdleMode(state: true, force: true);
			}
		}
	}

	private void Start()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		ReferenceHub.OnPlayerAdded += delegate
		{
			IdleMode.SetIdleMode(state: false);
		};
		ReferenceHub.OnPlayerRemoved += delegate
		{
			if (ReferenceHub.AllHubs.Count <= 1)
			{
				IdleMode.SetIdleMode(state: true);
			}
		};
	}

	private void FixedUpdate()
	{
		if (IdleMode._st.ElapsedMilliseconds >= IdleMode.IdleModeTime && !IdleMode._pauseIdleMode && (!IdleMode.PreauthStopwatch.IsRunning || IdleMode.PreauthStopwatch.ElapsedMilliseconds >= IdleMode.IdleModePreauthTime))
		{
			IdleMode._st.Reset();
			IdleMode.PreauthStopwatch.Reset();
			if (ReferenceHub.AllHubs.Count <= 1)
			{
				IdleMode.SetIdleMode(state: true);
			}
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
		IdleMode.SetIdleMode(state, force: false);
	}

	private static void SetIdleMode(bool state, bool force)
	{
		if (NetworkServer.active && (!state || !IdleMode._pauseIdleMode || force) && (state != IdleMode.IdleModeActive || force) && (!state || IdleMode.IdleModeEnabled) && ServerStatic.IsDedicated)
		{
			if (state)
			{
				Application.targetFrameRate = IdleMode.IdleModeTickrate;
				Time.timeScale = 0.01f;
				ServerConsole.AddLog("Server has entered the idle mode.");
				ServerConsole.AddOutputEntry(default(IdleEnterEntry));
			}
			else
			{
				Application.targetFrameRate = ServerStatic.ServerTickrate;
				Time.timeScale = 1f;
				ServerConsole.AddLog("Server has exited the idle mode.");
				ServerConsole.AddOutputEntry(default(IdleExitEntry));
			}
			IdleMode.IdleModeActive = state;
		}
	}
}
