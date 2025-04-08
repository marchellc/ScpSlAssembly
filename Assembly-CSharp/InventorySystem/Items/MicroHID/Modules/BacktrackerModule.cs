using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class BacktrackerModule : MicroHidModuleBase
	{
		private void UpdateTick()
		{
			FiringModeControllerModule firingModeControllerModule;
			if (!base.MicroHid.CycleController.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			MicroHidPhase phase = base.MicroHid.CycleController.Phase;
			if (phase != MicroHidPhase.WindingUp)
			{
				if (phase - MicroHidPhase.WoundUpSustain <= 1)
				{
					this.ClientSendUpdate(firingModeControllerModule);
					return;
				}
			}
			else
			{
				float windUpRate = firingModeControllerModule.WindUpRate;
				if (windUpRate > 0f && (1f - base.MicroHid.CycleController.ServerWindUpProgress) / windUpRate <= 0.3f)
				{
					this.ClientSendUpdate(firingModeControllerModule);
				}
			}
		}

		private void ClientSendUpdate(FiringModeControllerModule firingCtrl)
		{
			IFpcRole fpcRole = base.MicroHid.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			this._lastFiringMode = firingCtrl;
			this._lastOwnerFpc = fpcRole;
			this.SendCmd(new Action<NetworkWriter>(this.ClientWriteMessage));
		}

		private void ClientWriteMessage(NetworkWriter writer)
		{
			Vector3 position = this._lastOwnerFpc.FpcModule.Position;
			RelativePosition relativePosition = new RelativePosition(position);
			Transform playerCameraReference = base.MicroHid.Owner.PlayerCameraReference;
			writer.WriteRelativePosition(relativePosition);
			writer.WriteQuaternion(WaypointBase.GetRelativeRotation(relativePosition.WaypointId, playerCameraReference.rotation));
			float num = this._lastFiringMode.FiringRange + 4f;
			float num2 = num * num;
			float backtrackerDot = this._lastFiringMode.BacktrackerDot;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.isLocalPlayer)
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						float num3 = fpcRole.SqrDistanceTo(position);
						if (num3 <= num2 && (num3 <= 4f || fpcRole.GetDot(playerCameraReference, 0.5f) <= backtrackerDot))
						{
							writer.WriteReferenceHub(referenceHub);
							writer.WriteRelativePosition(new RelativePosition(fpcRole));
						}
					}
				}
			}
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			this._remainingCooldown -= Time.deltaTime;
			if (this._remainingCooldown > 0f)
			{
				return;
			}
			this.UpdateTick();
			this._remainingCooldown = 0.05f;
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._receivedBacktracks.Clear();
			this._receivedOwnerPosition = reader.ReadRelativePosition();
			this._receivedOwnerRotation = reader.ReadQuaternion();
			while (reader.Remaining > 0)
			{
				ReferenceHub referenceHub;
				bool flag = reader.TryReadReferenceHub(out referenceHub);
				RelativePosition relativePosition = reader.ReadRelativePosition();
				if (flag)
				{
					this._receivedBacktracks.Add(new BacktrackerModule.BacktrackPair(referenceHub, relativePosition));
				}
			}
			this._lastReceivedMessageTimestamp = NetworkTime.time;
		}

		public void BacktrackAll(Action callback)
		{
			WaypointBase waypointBase;
			if (NetworkTime.time - this._lastReceivedMessageTimestamp > 0.5 || !WaypointBase.TryGetWaypoint(this._receivedOwnerPosition.WaypointId, out waypointBase))
			{
				if (callback != null)
				{
					callback();
				}
				return;
			}
			Vector3 worldspacePosition = waypointBase.GetWorldspacePosition(this._receivedOwnerPosition.Relative);
			Quaternion worldspaceRotation = waypointBase.GetWorldspaceRotation(this._receivedOwnerRotation);
			using (new FpcBacktracker(base.MicroHid.Owner, worldspacePosition, worldspaceRotation, 0.1f, 0.15f))
			{
				foreach (BacktrackerModule.BacktrackPair backtrackPair in this._receivedBacktracks)
				{
					List<FpcBacktracker> victimBacktrackers = BacktrackerModule.VictimBacktrackers;
					ReferenceHub hub = backtrackPair.Hub;
					RelativePosition relPos = backtrackPair.RelPos;
					victimBacktrackers.Add(new FpcBacktracker(hub, relPos.Position, 0.4f));
				}
				if (callback != null)
				{
					callback();
				}
				foreach (FpcBacktracker fpcBacktracker2 in BacktrackerModule.VictimBacktrackers)
				{
					fpcBacktracker2.RestorePosition();
				}
				BacktrackerModule.VictimBacktrackers.Clear();
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

		private readonly List<BacktrackerModule.BacktrackPair> _receivedBacktracks = new List<BacktrackerModule.BacktrackPair>();

		private static readonly List<FpcBacktracker> VictimBacktrackers = new List<FpcBacktracker>();

		private struct BacktrackPair
		{
			public BacktrackPair(ReferenceHub hub, RelativePosition pos)
			{
				this.Hub = hub;
				this.RelPos = pos;
			}

			public ReferenceHub Hub;

			public RelativePosition RelPos;
		}
	}
}
