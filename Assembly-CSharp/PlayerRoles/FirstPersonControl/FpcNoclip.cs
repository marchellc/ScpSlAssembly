using System;
using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem;
using Mirror;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcNoclip
	{
		public bool IsActive
		{
			get
			{
				return this._stats.HasFlag(AdminFlags.Noclip);
			}
			set
			{
				this._stats.SetFlag(AdminFlags.Noclip, value);
			}
		}

		public bool RecentlyActive
		{
			get
			{
				return this._lastNcSw.IsRunning && this._lastNcSw.Elapsed.TotalSeconds < 2.5;
			}
		}

		public FpcNoclip(ReferenceHub hub, FirstPersonMovementModule fpmm)
		{
			this._hub = hub;
			this._fpmm = fpmm;
			this._stats = hub.playerStats.GetModule<AdminFlagsStat>();
			this._lastNcSw = new Stopwatch();
			if (this._hub.isLocalPlayer)
			{
				FpcNoclip.ReloadInputConfigs();
			}
		}

		public void UpdateNoclip()
		{
			if (this._hub.isLocalPlayer && Input.GetKeyDown(FpcNoclip._keyToggle))
			{
				NetworkClient.Send<FpcNoclipToggleMessage>(default(FpcNoclipToggleMessage), 0);
			}
			if (!this._stats.HasFlag(AdminFlags.Noclip))
			{
				if (this._wasEnabled)
				{
					this.DisableNoclipClientside();
				}
				this._wasEnabled = false;
				return;
			}
			this._wasEnabled = true;
			this._lastNcSw.Restart();
			if (NetworkServer.active)
			{
				this._fpmm.Motor.ResetFallDamageCooldown();
			}
			if (!this._hub.isLocalPlayer)
			{
				Vector3 position = this._fpmm.Motor.ReceivedPosition.Position;
				float num = (((position - this._fpmm.Position).sqrMagnitude > 25f) ? 1f : (16f * Time.deltaTime));
				this._fpmm.Position = Vector3.Lerp(this._fpmm.Position, position, num);
				return;
			}
		}

		public void ShutdownModule()
		{
			if (NetworkServer.active)
			{
				this.IsActive = false;
			}
			this.DisableNoclipClientside();
		}

		private void DisableNoclipClientside()
		{
		}

		public static bool IsPermitted(ReferenceHub ply)
		{
			return ply != null && FpcNoclip.PermittedPlayers.Contains(ply.netId);
		}

		public static void PermitPlayer(ReferenceHub ply)
		{
			if (ply == null)
			{
				return;
			}
			FpcNoclip.PermittedPlayers.Add(ply.netId);
			ply.gameConsoleTransmission.SendToClient("Noclip is now permitted.", "green");
		}

		public static void UnpermitPlayer(ReferenceHub ply)
		{
			if (ply == null)
			{
				return;
			}
			FpcNoclip.PermittedPlayers.Remove(ply.netId);
			ply.playerStats.GetModule<AdminFlagsStat>().SetFlag(AdminFlags.Noclip, false);
			ply.gameConsoleTransmission.SendToClient("Noclip permission revoked.", "yellow");
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnServerStarted += FpcNoclip.PermittedPlayers.Clear;
			NewInput.OnAnyModified += FpcNoclip.ReloadInputConfigs;
		}

		private static void ReloadInputConfigs()
		{
		}

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
	}
}
