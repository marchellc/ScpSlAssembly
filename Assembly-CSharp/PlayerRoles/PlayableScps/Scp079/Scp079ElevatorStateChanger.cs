using System;
using System.Collections.Generic;
using AudioPooling;
using Interactables.Interobjects;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079ElevatorStateChanger : Scp079KeyAbilityBase
{
	[SerializeField]
	private AudioClip _confirmationSound;

	[SerializeField]
	private int _cost;

	private ElevatorDoor _lastElevator;

	private Scp079HudTranslation _failedReason;

	private string _abilityName;

	public override bool IsVisible
	{
		get
		{
			if (Scp079CursorManager.LockCameras)
			{
				return false;
			}
			if (OverconManager.Singleton.HighlightedOvercon is ElevatorOvercon elevatorOvercon && elevatorOvercon != null)
			{
				this._lastElevator = elevatorOvercon.Target;
				return true;
			}
			return false;
		}
	}

	public override bool IsReady
	{
		get
		{
			if (this.ErrorCode == Scp079HudTranslation.Zoom && ElevatorChamber.TryGetChamber(this._lastElevator.Group, out var chamber))
			{
				return chamber.IsReady;
			}
			return false;
		}
	}

	public override string FailMessage => this._failedReason switch
	{
		Scp079HudTranslation.Zoom => null, 
		Scp079HudTranslation.NotEnoughAux => base.GetNoAuxMessage(this._cost), 
		_ => Translations.Get(this._failedReason), 
	};

	public override ActionName ActivationKey => ActionName.Shoot;

	public override string AbilityName => string.Format(this._abilityName, this._cost);

	private Scp079HudTranslation ErrorCode
	{
		get
		{
			if (base.AuxManager.CurrentAux < (float)this._cost)
			{
				return Scp079HudTranslation.NotEnoughAux;
			}
			if (!this.ValidateLastElevator)
			{
				return Scp079HudTranslation.ElevatorAccessDenied;
			}
			return Scp079HudTranslation.Zoom;
		}
	}

	private bool ValidateLastElevator
	{
		get
		{
			if (this._lastElevator == null)
			{
				return false;
			}
			return ElevatorDoor.GetDoorsForGroup(this._lastElevator.Group).Any((ElevatorDoor x) => x.ActiveLocks == 0);
		}
	}

	public static event Action<Scp079Role, ElevatorDoor> OnServerElevatorDoorClosed;

	protected override void Start()
	{
		base.Start();
		this._abilityName = Translations.Get(Scp079HudTranslation.SendElevator);
		base.CurrentCamSync.OnCameraChanged += delegate
		{
			this._failedReason = Scp079HudTranslation.Zoom;
		};
	}

	protected override void Trigger()
	{
		base.ClientSendCmd();
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteByte((byte)this._lastElevator.Group);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (base.AuxManager.CurrentAux < (float)this._cost || base.LostSignalHandler.Lost)
		{
			return;
		}
		ElevatorGroup elevatorGroup = (ElevatorGroup)reader.ReadByte();
		if (!ElevatorChamber.TryGetChamber(elevatorGroup, out var chamber) || !chamber.IsReadyForUserInput)
		{
			return;
		}
		List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(elevatorGroup);
		if (doorsForGroup.All((ElevatorDoor x) => x.ActiveLocks != 0))
		{
			return;
		}
		RoomIdentifier curRoom = base.CurrentCamSync.CurrentCamera.Room;
		if (doorsForGroup.TryGetFirst((ElevatorDoor x) => x.Rooms.Contains(curRoom), out var first))
		{
			bool targetState = first.TargetState;
			chamber.ServerSetDestination(chamber.NextLevel, allowQueueing: false);
			base.AuxManager.CurrentAux -= this._cost;
			doorsForGroup.ForEach(delegate(ElevatorDoor x)
			{
				base.RewardManager.MarkRooms(x.Rooms);
			});
			base.ServerSendRpc(toAll: false);
			if (targetState)
			{
				Scp079ElevatorStateChanger.OnServerElevatorDoorClosed?.Invoke(base.CastRole, first);
			}
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		AudioSourcePoolManager.Play2D(this._confirmationSound);
	}

	public override void OnFailMessageAssigned()
	{
		this._failedReason = this.ErrorCode;
	}
}
