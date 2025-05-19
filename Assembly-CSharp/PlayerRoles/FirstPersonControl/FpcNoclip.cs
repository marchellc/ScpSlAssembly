using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem;
using Mirror;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl;

public class FpcNoclip
{
	public static float CurSpeed = 10f;

	private const float DefaultNoclipSpeed = 10f;

	private const float MinNoclipSpeed = 0.1f;

	private const float MaxNoclipSpeed = 250f;

	private const float NoclipLerp = 16f;

	private const float NoclipMaxDiffSqrt = 25f;

	private const float RecentTimeThreshold = 2.5f;

	private const string AxisName = "Mouse ScrollWheel";

	private bool _wasEnabled;

	private readonly ReferenceHub _hub;

	private readonly FirstPersonMovementModule _fpmm;

	private readonly AdminFlagsStat _stats;

	private readonly Stopwatch _lastNcSw;

	private static readonly HashSet<uint> PermittedPlayers = new HashSet<uint>();

	private static KeyCode _keyFwd;

	private static KeyCode _keyBwd;

	private static KeyCode _keyLft;

	private static KeyCode _keyRgt;

	private static KeyCode _keyUpw;

	private static KeyCode _keyDnw;

	private static KeyCode _keyToggle;

	private static KeyCode _keyFog;

	public bool IsActive
	{
		get
		{
			return _stats.HasFlag(AdminFlags.Noclip);
		}
		set
		{
			_stats.SetFlag(AdminFlags.Noclip, value);
		}
	}

	public bool RecentlyActive
	{
		get
		{
			if (_lastNcSw.IsRunning)
			{
				return _lastNcSw.Elapsed.TotalSeconds < 2.5;
			}
			return false;
		}
	}

	public FpcNoclip(ReferenceHub hub, FirstPersonMovementModule fpmm)
	{
		_hub = hub;
		_fpmm = fpmm;
		_stats = hub.playerStats.GetModule<AdminFlagsStat>();
		_lastNcSw = new Stopwatch();
		if (_hub.isLocalPlayer)
		{
			ReloadInputConfigs();
		}
	}

	public void UpdateNoclip()
	{
		if (_hub.isLocalPlayer && Input.GetKeyDown(_keyToggle))
		{
			NetworkClient.Send(default(FpcNoclipToggleMessage));
		}
		if (!_stats.HasFlag(AdminFlags.Noclip))
		{
			if (_wasEnabled)
			{
				DisableNoclipClientside();
			}
			_wasEnabled = false;
			return;
		}
		_wasEnabled = true;
		_lastNcSw.Restart();
		if (NetworkServer.active)
		{
			_fpmm.Motor.ResetFallDamageCooldown();
		}
		if (!_hub.isLocalPlayer)
		{
			Vector3 position = _fpmm.Motor.ReceivedPosition.Position;
			float t = (((position - _fpmm.Position).sqrMagnitude > 25f) ? 1f : (16f * Time.deltaTime));
			_fpmm.Position = Vector3.Lerp(_fpmm.Position, position, t);
		}
	}

	public void ShutdownModule()
	{
		if (NetworkServer.active)
		{
			IsActive = false;
		}
		DisableNoclipClientside();
	}

	private void DisableNoclipClientside()
	{
	}

	public static bool IsPermitted(ReferenceHub ply)
	{
		if (ply != null)
		{
			return PermittedPlayers.Contains(ply.netId);
		}
		return false;
	}

	public static void PermitPlayer(ReferenceHub ply)
	{
		if (!(ply == null))
		{
			PermittedPlayers.Add(ply.netId);
			ply.gameConsoleTransmission.SendToClient("Noclip is now permitted.", "green");
		}
	}

	public static void UnpermitPlayer(ReferenceHub ply)
	{
		if (!(ply == null))
		{
			PermittedPlayers.Remove(ply.netId);
			ply.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.Noclip, status: false);
			ply.gameConsoleTransmission.SendToClient("Noclip permission revoked.", "yellow");
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Inventory.OnServerStarted += PermittedPlayers.Clear;
		NewInput.OnAnyModified += ReloadInputConfigs;
	}

	private static void ReloadInputConfigs()
	{
	}
}
