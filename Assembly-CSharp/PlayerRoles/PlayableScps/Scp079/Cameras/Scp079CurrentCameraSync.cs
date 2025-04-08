using System;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public class Scp079CurrentCameraSync : StandardSubroutine<Scp079Role>, IScp079FailMessageProvider
	{
		public event Action OnCameraChanged;

		public string FailMessage { get; private set; }

		public Scp079CurrentCameraSync.ClientSwitchState CurClientSwitchState { get; private set; }

		public FacilityZone CurClientTargetZone { get; private set; }

		public Scp079Camera CurrentCamera
		{
			get
			{
				Scp079Camera scp079Camera;
				this.TryGetCurrentCamera(out scp079Camera);
				return scp079Camera;
			}
			private set
			{
				value.IsActive = true;
				this._camSet = true;
				if (value == this._lastCam)
				{
					return;
				}
				this._lastCam = value;
				Action onCameraChanged = this.OnCameraChanged;
				if (onCameraChanged != null)
				{
					onCameraChanged();
				}
				if (NetworkServer.active)
				{
					base.ServerSendRpc(true);
				}
			}
		}

		private void Update()
		{
			if (!this._initialized)
			{
				Scp079Camera scp079Camera;
				if (!this.TryGetCurrentCamera(out scp079Camera))
				{
					return;
				}
				this._initialized = true;
			}
			if (this.CurClientSwitchState == Scp079CurrentCameraSync.ClientSwitchState.None || !base.Role.IsLocalPlayer)
			{
				return;
			}
			if (this._switchStopwatch.Elapsed.TotalSeconds < (double)this._targetSwitchTime)
			{
				return;
			}
			this._clientSwitchRequest = Scp079CurrentCameraSync.ClientSwitchState.None;
			this._switchStopwatch.Stop();
			base.ClientSendCmd();
		}

		private bool TryGetDefaultCamera(out Scp079Camera cam)
		{
			if (this._defaultCamId == 0)
			{
				Scp079InteractableBase scp079InteractableBase;
				if (Scp079InteractableBase.AllInstances.TryGetFirst(delegate(Scp079InteractableBase x)
				{
					Scp079Camera scp079Camera2 = x as Scp079Camera;
					return scp079Camera2 != null && x.SyncId != 0 && scp079Camera2.Room.Name == RoomName.Hcz079 && scp079Camera2.Label.Equals("079 CONT CHAMBER", StringComparison.InvariantCultureIgnoreCase);
				}, out scp079InteractableBase))
				{
					cam = scp079InteractableBase as Scp079Camera;
					this._defaultCamId = cam.SyncId;
					return true;
				}
				Scp079InteractableBase scp079InteractableBase2;
				if (Scp079InteractableBase.AllInstances.TryGetFirst((Scp079InteractableBase x) => x is Scp079Camera, out scp079InteractableBase2))
				{
					cam = scp079InteractableBase2 as Scp079Camera;
					this._defaultCamId = cam.SyncId;
					return true;
				}
				cam = null;
				return false;
			}
			else
			{
				Scp079Camera scp079Camera;
				if (Scp079InteractableBase.TryGetInteractable<Scp079Camera>(this._defaultCamId, out scp079Camera))
				{
					cam = scp079Camera;
					return true;
				}
				cam = null;
				return false;
			}
		}

		private void OnHubAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.ServerSendRpc(hub);
		}

		public int GetSwitchCost(Scp079Camera target)
		{
			Vector3 position = this.CurrentCamera.Position;
			Vector3 position2 = target.Position;
			FacilityZone zone = this.CurrentCamera.Room.Zone;
			FacilityZone zone2 = target.Room.Zone;
			bool flag = Mathf.Abs(position.y - position2.y) < 60f;
			bool flag2 = zone == zone2;
			int num = Mathf.CeilToInt((position - position2).MagnitudeIgnoreY() * 0.16f);
			int num2 = ((!flag) ? 5 : num);
			if (!flag2)
			{
				int num3 = Mathf.Abs(Scp079CurrentCameraSync.ZoneQueue.IndexOf(zone) - Scp079CurrentCameraSync.ZoneQueue.IndexOf(zone2)) - 1;
				return num2 + 10 + 20 * num3;
			}
			Vector3Int vector3Int;
			if (!this.CurrentCamera.Room.TryGetMainCoords(out vector3Int))
			{
				return num2;
			}
			Vector3Int vector3Int2;
			if (!target.Room.TryGetMainCoords(out vector3Int2))
			{
				return num2;
			}
			bool flag3 = (vector3Int - vector3Int2).sqrMagnitude <= 1;
			if (!flag)
			{
				if (!flag3)
				{
					num2 += num;
				}
				return num2;
			}
			if (!flag3)
			{
				return num2;
			}
			return 0;
		}

		public void ClientSwitchTo(Scp079Camera target)
		{
			if (!base.Role.IsLocalPlayer)
			{
				throw new InvalidOperationException("Method ClientSwitchTo can only be called on the local client instance!");
			}
			if (this.CurClientSwitchState != Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				return;
			}
			bool flag = target.Room.Zone == this.CurrentCamera.Room.Zone;
			this._switchTarget = target;
			this._targetSwitchTime = (flag ? 0.1f : 0.98f);
			this.CurClientSwitchState = (flag ? Scp079CurrentCameraSync.ClientSwitchState.SwitchingRoom : Scp079CurrentCameraSync.ClientSwitchState.SwitchingZone);
			this.CurClientTargetZone = target.Room.Zone;
			this._clientSwitchRequest = this.CurClientSwitchState;
			base.ClientSendCmd();
			this._switchStopwatch.Restart();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			if (!NetworkServer.active)
			{
				return;
			}
			this._eventAssigned = true;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnHubAdded));
			(base.Role as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out this._auxManager);
			(base.Role as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._lostSignalHandler);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			if (this._eventAssigned)
			{
				ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnHubAdded));
			}
			this._defaultCamId = 0;
			this._errorCode = Scp079HudTranslation.Zoom;
			this._lastCam = null;
			this._camSet = false;
			this._initialized = false;
			this._eventAssigned = false;
			this.CurClientSwitchState = Scp079CurrentCameraSync.ClientSwitchState.None;
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteByte((byte)this._clientSwitchRequest);
			writer.WriteUShort(this._switchTarget.SyncId);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._clientSwitchRequest = (Scp079CurrentCameraSync.ClientSwitchState)reader.ReadByte();
			this._requestedCamId = reader.ReadUShort();
			if (this._clientSwitchRequest != Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				base.ServerSendRpc((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole);
				return;
			}
			if (!Scp079InteractableBase.TryGetInteractable<Scp079Camera>(this._requestedCamId, out this._switchTarget))
			{
				this._errorCode = Scp079HudTranslation.InvalidCamera;
				base.ServerSendRpc(true);
				return;
			}
			float num = (float)this.GetSwitchCost(this._switchTarget);
			if (num > this._auxManager.CurrentAux)
			{
				this._errorCode = Scp079HudTranslation.NotEnoughAux;
				base.ServerSendRpc(true);
				return;
			}
			if (this._lostSignalHandler.Lost)
			{
				this._errorCode = Scp079HudTranslation.SignalLost;
				base.ServerSendRpc(true);
				return;
			}
			if (this._switchTarget != this.CurrentCamera)
			{
				Scp079ChangingCameraEventArgs scp079ChangingCameraEventArgs = new Scp079ChangingCameraEventArgs(base.Owner, this._switchTarget);
				Scp079Events.OnChangingCamera(scp079ChangingCameraEventArgs);
				if (!scp079ChangingCameraEventArgs.IsAllowed)
				{
					this._errorCode = Scp079HudTranslation.SignalLost;
					base.ServerSendRpc(true);
					return;
				}
				this._switchTarget = scp079ChangingCameraEventArgs.Camera;
			}
			this._auxManager.CurrentAux -= num;
			this._errorCode = Scp079HudTranslation.Zoom;
			if (this._switchTarget != this.CurrentCamera)
			{
				this.CurrentCamera = this._switchTarget;
				Scp079Events.OnChangedCamera(new Scp079ChangedCameraEventArgs(base.Owner, this._switchTarget));
				return;
			}
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._clientSwitchRequest);
			if (this._clientSwitchRequest == Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				writer.WriteUShort(this.CurrentCamera.SyncId);
				writer.WriteByte((byte)this._errorCode);
			}
			else
			{
				writer.WriteUShort(this._requestedCamId);
			}
			this._errorCode = Scp079HudTranslation.Zoom;
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.CurClientSwitchState = (Scp079CurrentCameraSync.ClientSwitchState)reader.ReadByte();
			ushort num = reader.ReadUShort();
			Scp079Camera scp079Camera;
			bool flag = Scp079InteractableBase.TryGetInteractable<Scp079Camera>(num, out scp079Camera);
			Scp079CurrentCameraSync.ClientSwitchState curClientSwitchState = this.CurClientSwitchState;
			if (curClientSwitchState == Scp079CurrentCameraSync.ClientSwitchState.SwitchingRoom)
			{
				return;
			}
			if (curClientSwitchState != Scp079CurrentCameraSync.ClientSwitchState.SwitchingZone)
			{
				this._errorCode = (Scp079HudTranslation)reader.ReadByte();
				if (this._errorCode != Scp079HudTranslation.Zoom)
				{
					if (base.Role.IsLocalPlayer || base.CastRole.IsSpectated)
					{
						Scp079AbilityList.Singleton.TrackedFailMessage = this;
					}
					return;
				}
				if (NetworkServer.active)
				{
					return;
				}
				if (flag)
				{
					this.CurrentCamera = scp079Camera;
					return;
				}
				this._camSet = false;
				this._defaultCamId = num;
				return;
			}
			else
			{
				if (!flag)
				{
					return;
				}
				this.CurClientTargetZone = scp079Camera.Room.Zone;
				return;
			}
		}

		public bool TryGetCurrentCamera(out Scp079Camera cam)
		{
			if (base.Role.Pooled)
			{
				cam = null;
				return false;
			}
			if (this._camSet)
			{
				cam = this._lastCam;
				return true;
			}
			Scp079Camera scp079Camera;
			if (this.TryGetDefaultCamera(out scp079Camera))
			{
				cam = scp079Camera;
				this.CurrentCamera = cam;
				return true;
			}
			cam = null;
			return false;
		}

		public void OnFailMessageAssigned()
		{
			this.FailMessage = Translations.Get<Scp079HudTranslation>(this._errorCode);
		}

		public const float CostPerMeter = 0.16f;

		public const int CostPerFloor = 5;

		public const int CostPerZone = 10;

		public const int CostPerSkippedZone = 20;

		public static readonly FacilityZone[] ZoneQueue = new FacilityZone[]
		{
			FacilityZone.Surface,
			FacilityZone.Entrance,
			FacilityZone.HeavyContainment,
			FacilityZone.LightContainment
		};

		private const float FloorHeight = 60f;

		private const int ErrorTranslationId = 2;

		private const float SameRoomSwitchDuration = 0.1f;

		private const float ZoneSwitchDuration = 0.98f;

		private const string DefaultCameraName = "079 CONT CHAMBER";

		private readonly Stopwatch _switchStopwatch = new Stopwatch();

		private Scp079Camera _lastCam;

		private Scp079Camera _switchTarget;

		private Scp079AuxManager _auxManager;

		private Scp079LostSignalHandler _lostSignalHandler;

		private bool _camSet;

		private bool _eventAssigned;

		private float _targetSwitchTime;

		private ushort _defaultCamId;

		private ushort _requestedCamId;

		private bool _initialized;

		private Scp079HudTranslation _errorCode;

		private Scp079CurrentCameraSync.ClientSwitchState _clientSwitchRequest;

		public enum ClientSwitchState
		{
			None,
			SwitchingRoom,
			SwitchingZone
		}
	}
}
