using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.MicroHID.Modules;

public class BacktrackerModule : MicroHidModuleBase
{
	private struct BacktrackPair
	{
		public ReferenceHub Hub;

		public RelativePosition RelPos;

		public BacktrackPair(ReferenceHub hub, RelativePosition pos)
		{
			this.Hub = hub;
			this.RelPos = pos;
		}
	}

	private const float TickCooldown = 0.05f;

	private const float WindupWarmupDuration = 0.3f;

	private const float AlwaysIncludeDistSqr = 4f;

	private const float BacktrackExpirationSeconds = 0.5f;

	private const float BacktrackExtraDistance = 4f;

	private float _remainingCooldown;

	private double _lastReceivedMessageTimestamp;

	private FiringModeControllerModule _lastFiringMode;

	private IFpcRole _lastOwnerFpc;

	private RelativePosition _receivedOwnerPosition;

	private Quaternion _receivedOwnerRotation;

	private readonly List<BacktrackPair> _receivedBacktracks = new List<BacktrackPair>();

	private static readonly List<FpcBacktracker> VictimBacktrackers = new List<FpcBacktracker>();

	private void UpdateTick()
	{
		if (!base.MicroHid.CycleController.TryGetLastFiringController(out var ret))
		{
			return;
		}
		switch (base.MicroHid.CycleController.Phase)
		{
		case MicroHidPhase.WoundUpSustain:
		case MicroHidPhase.Firing:
			this.ClientSendUpdate(ret);
			break;
		case MicroHidPhase.WindingUp:
		{
			float windUpRate = ret.WindUpRate;
			if (!(windUpRate <= 0f) && !((1f - base.MicroHid.CycleController.ServerWindUpProgress) / windUpRate > 0.3f))
			{
				this.ClientSendUpdate(ret);
			}
			break;
		}
		}
	}

	private void ClientSendUpdate(FiringModeControllerModule firingCtrl)
	{
		if (base.MicroHid.Owner.roleManager.CurrentRole is IFpcRole lastOwnerFpc)
		{
			this._lastFiringMode = firingCtrl;
			this._lastOwnerFpc = lastOwnerFpc;
			this.SendCmd(ClientWriteMessage);
		}
	}

	private void ClientWriteMessage(NetworkWriter writer)
	{
		Vector3 position = this._lastOwnerFpc.FpcModule.Position;
		RelativePosition msg = new RelativePosition(position);
		Transform playerCameraReference = base.MicroHid.Owner.PlayerCameraReference;
		writer.WriteRelativePosition(msg);
		writer.WriteQuaternion(WaypointBase.GetRelativeRotation(msg.WaypointId, playerCameraReference.rotation));
		float num = this._lastFiringMode.FiringRange + 4f;
		float num2 = num * num;
		float backtrackerDot = this._lastFiringMode.BacktrackerDot;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.isLocalPlayer && allHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				float num3 = fpcRole.SqrDistanceTo(position);
				if (!(num3 > num2) && (num3 <= 4f || fpcRole.GetDot(playerCameraReference) <= backtrackerDot))
				{
					writer.WriteReferenceHub(allHub);
					writer.WriteRelativePosition(new RelativePosition(fpcRole));
				}
			}
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsLocalPlayer)
		{
			this._remainingCooldown -= Time.deltaTime;
			if (!(this._remainingCooldown > 0f))
			{
				this.UpdateTick();
				this._remainingCooldown = 0.05f;
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this._receivedBacktracks.Clear();
		this._receivedOwnerPosition = reader.ReadRelativePosition();
		this._receivedOwnerRotation = reader.ReadQuaternion();
		while (reader.Remaining > 0)
		{
			ReferenceHub hub;
			bool num = reader.TryReadReferenceHub(out hub);
			RelativePosition pos = reader.ReadRelativePosition();
			if (num)
			{
				this._receivedBacktracks.Add(new BacktrackPair(hub, pos));
			}
		}
		this._lastReceivedMessageTimestamp = NetworkTime.time;
	}

	public void BacktrackAll(Action callback)
	{
		if (NetworkTime.time - this._lastReceivedMessageTimestamp > 0.5 || !WaypointBase.TryGetWaypoint(this._receivedOwnerPosition.WaypointId, out var wp))
		{
			callback?.Invoke();
			return;
		}
		Vector3 worldspacePosition = wp.GetWorldspacePosition(this._receivedOwnerPosition.Relative);
		Quaternion worldspaceRotation = wp.GetWorldspaceRotation(this._receivedOwnerRotation);
		using (new FpcBacktracker(base.MicroHid.Owner, worldspacePosition, worldspaceRotation))
		{
			foreach (BacktrackPair receivedBacktrack in this._receivedBacktracks)
			{
				List<FpcBacktracker> victimBacktrackers = BacktrackerModule.VictimBacktrackers;
				ReferenceHub hub = receivedBacktrack.Hub;
				RelativePosition relPos = receivedBacktrack.RelPos;
				victimBacktrackers.Add(new FpcBacktracker(hub, relPos.Position));
			}
			callback?.Invoke();
			foreach (FpcBacktracker victimBacktracker in BacktrackerModule.VictimBacktrackers)
			{
				victimBacktracker.RestorePosition();
			}
			BacktrackerModule.VictimBacktrackers.Clear();
		}
	}
}
