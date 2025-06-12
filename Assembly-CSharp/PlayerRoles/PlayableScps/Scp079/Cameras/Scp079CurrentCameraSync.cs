using System;
using System.Diagnostics;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Cameras;

public class Scp079CurrentCameraSync : StandardSubroutine<Scp079Role>, IScp079FailMessageProvider
{
	public enum ClientSwitchState
	{
		None,
		SwitchingRoom,
		SwitchingZone
	}

	public const float CostPerMeter = 0.16f;

	public const int CostPerFloor = 5;

	public const int CostPerZone = 10;

	public const int CostPerSkippedZone = 20;

	public static readonly FacilityZone[] ZoneQueue = new FacilityZone[4]
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

	private ClientSwitchState _clientSwitchRequest;

	public string FailMessage { get; private set; }

	public ClientSwitchState CurClientSwitchState { get; private set; }

	public FacilityZone CurClientTargetZone { get; private set; }

	public Scp079Camera CurrentCamera
	{
		get
		{
			this.TryGetCurrentCamera(out var cam);
			return cam;
		}
		private set
		{
			value.IsActive = true;
			this._camSet = true;
			if (!(value == this._lastCam))
			{
				this._lastCam = value;
				this.OnCameraChanged?.Invoke();
				if (NetworkServer.active)
				{
					base.ServerSendRpc(toAll: true);
				}
			}
		}
	}

	public event Action OnCameraChanged;

	private void Update()
	{
		if (!this._initialized)
		{
			if (!this.TryGetCurrentCamera(out var _))
			{
				return;
			}
			this._initialized = true;
		}
		if (this.CurClientSwitchState != ClientSwitchState.None && base.Role.IsControllable && !(this._switchStopwatch.Elapsed.TotalSeconds < (double)this._targetSwitchTime))
		{
			this._clientSwitchRequest = ClientSwitchState.None;
			this._switchStopwatch.Stop();
			base.ClientSendCmd();
		}
	}

	private bool TryGetDefaultCamera(out Scp079Camera cam)
	{
		if (this._defaultCamId == 0)
		{
			if (Scp079InteractableBase.AllInstances.TryGetFirst((Scp079InteractableBase x) => x is Scp079Camera scp079Camera && x.SyncId != 0 && scp079Camera.Room.Name == RoomName.Hcz079 && scp079Camera.Label.Equals("079 CONT CHAMBER", StringComparison.InvariantCultureIgnoreCase), out var first))
			{
				cam = first as Scp079Camera;
				this._defaultCamId = cam.SyncId;
				return true;
			}
			if (Scp079InteractableBase.AllInstances.TryGetFirst((Scp079InteractableBase x) => x is Scp079Camera, out var first2))
			{
				cam = first2 as Scp079Camera;
				this._defaultCamId = cam.SyncId;
				return true;
			}
			cam = null;
			return false;
		}
		if (Scp079InteractableBase.TryGetInteractable(this._defaultCamId, out Scp079Camera result))
		{
			cam = result;
			return true;
		}
		cam = null;
		return false;
	}

	private void OnHubAdded(ReferenceHub hub)
	{
		if (NetworkServer.active)
		{
			base.ServerSendRpc(hub);
		}
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
		if (flag2)
		{
			Vector3Int mainCoords = this.CurrentCamera.Room.MainCoords;
			Vector3Int mainCoords2 = target.Room.MainCoords;
			bool flag3 = (mainCoords - mainCoords2).sqrMagnitude <= 1;
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
		int num3 = Mathf.Abs(Scp079CurrentCameraSync.ZoneQueue.IndexOf(zone) - Scp079CurrentCameraSync.ZoneQueue.IndexOf(zone2)) - 1;
		return num2 + 10 + 20 * num3;
	}

	public void ClientSwitchTo(Scp079Camera target)
	{
		if (!base.Role.IsControllable)
		{
			throw new InvalidOperationException("Method ClientSwitchTo can only be called on the local client instance!");
		}
		if (this.CurClientSwitchState == ClientSwitchState.None)
		{
			bool flag = target.Room.Zone == this.CurrentCamera.Room.Zone;
			this._switchTarget = target;
			this._targetSwitchTime = (flag ? 0.1f : 0.98f);
			this.CurClientSwitchState = (flag ? ClientSwitchState.SwitchingRoom : ClientSwitchState.SwitchingZone);
			this.CurClientTargetZone = target.Room.Zone;
			this._clientSwitchRequest = this.CurClientSwitchState;
			base.ClientSendCmd();
			this._switchStopwatch.Restart();
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (NetworkServer.active)
		{
			this._eventAssigned = true;
			ReferenceHub.OnPlayerAdded += OnHubAdded;
			(base.Role as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out this._auxManager);
			(base.Role as Scp079Role).SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._lostSignalHandler);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		if (this._eventAssigned)
		{
			ReferenceHub.OnPlayerAdded -= OnHubAdded;
		}
		this._defaultCamId = 0;
		this._errorCode = Scp079HudTranslation.Zoom;
		this._lastCam = null;
		this._camSet = false;
		this._initialized = false;
		this._eventAssigned = false;
		this.CurClientSwitchState = ClientSwitchState.None;
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
		this._clientSwitchRequest = (ClientSwitchState)reader.ReadByte();
		this._requestedCamId = reader.ReadUShort();
		if (this._clientSwitchRequest != ClientSwitchState.None)
		{
			base.ServerSendRpc((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole);
			return;
		}
		if (!Scp079InteractableBase.TryGetInteractable(this._requestedCamId, out this._switchTarget))
		{
			this._errorCode = Scp079HudTranslation.InvalidCamera;
			base.ServerSendRpc(toAll: true);
			return;
		}
		float num = this.GetSwitchCost(this._switchTarget);
		if (num > this._auxManager.CurrentAux)
		{
			this._errorCode = Scp079HudTranslation.NotEnoughAux;
			base.ServerSendRpc(toAll: true);
			return;
		}
		if (this._lostSignalHandler.Lost)
		{
			this._errorCode = Scp079HudTranslation.SignalLost;
			base.ServerSendRpc(toAll: true);
			return;
		}
		if (this._switchTarget != this.CurrentCamera)
		{
			Scp079ChangingCameraEventArgs e = new Scp079ChangingCameraEventArgs(base.Owner, this._switchTarget);
			Scp079Events.OnChangingCamera(e);
			if (!e.IsAllowed)
			{
				this._errorCode = Scp079HudTranslation.SignalLost;
				base.ServerSendRpc(toAll: true);
				return;
			}
			this._switchTarget = e.Camera.Base;
		}
		this._auxManager.CurrentAux -= num;
		this._errorCode = Scp079HudTranslation.Zoom;
		if (this._switchTarget != this.CurrentCamera)
		{
			this.CurrentCamera = this._switchTarget;
			Scp079Events.OnChangedCamera(new Scp079ChangedCameraEventArgs(base.Owner, this._switchTarget));
		}
		else
		{
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._clientSwitchRequest);
		if (this._clientSwitchRequest == ClientSwitchState.None)
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
		this.CurClientSwitchState = (ClientSwitchState)reader.ReadByte();
		ushort num = reader.ReadUShort();
		Scp079Camera result;
		bool flag = Scp079InteractableBase.TryGetInteractable(num, out result);
		switch (this.CurClientSwitchState)
		{
		case ClientSwitchState.SwitchingRoom:
			return;
		case ClientSwitchState.SwitchingZone:
			if (flag)
			{
				this.CurClientTargetZone = result.Room.Zone;
			}
			return;
		}
		this._errorCode = (Scp079HudTranslation)reader.ReadByte();
		if (this._errorCode != Scp079HudTranslation.Zoom)
		{
			if ((base.Role.IsControllable || base.CastRole.IsSpectated) && Scp079AbilityList.TryGetSingleton(out var singleton))
			{
				singleton.TrackedFailMessage = this;
			}
		}
		else if (!NetworkServer.active)
		{
			if (flag)
			{
				this.CurrentCamera = result;
				return;
			}
			this._camSet = false;
			this._defaultCamId = num;
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
		if (this.TryGetDefaultCamera(out var cam2))
		{
			cam = cam2;
			this.CurrentCamera = cam;
			return true;
		}
		cam = null;
		return false;
	}

	public void OnFailMessageAssigned()
	{
		this.FailMessage = Translations.Get(this._errorCode);
	}

	public override void PopulateDummyActions(Action<DummyAction> actionAdder, Action<string> categoryAdder)
	{
		base.PopulateDummyActions(actionAdder, categoryAdder);
		categoryAdder("CameraSwitch");
		foreach (Scp079InteractableBase allInstance in Scp079InteractableBase.AllInstances)
		{
			Scp079Camera cam = allInstance as Scp079Camera;
			if ((object)cam != null)
			{
				actionAdder(new DummyAction($"\"{cam.Label}\" #{cam.SyncId}", delegate
				{
					this.ClientSwitchTo(cam);
				}));
			}
		}
	}
}
