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

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ElevatorStateChanger : Scp079KeyAbilityBase
	{
		public static event Action<Scp079Role, ElevatorDoor> OnServerElevatorDoorClosed;

		public override bool IsVisible
		{
			get
			{
				if (Scp079CursorManager.LockCameras)
				{
					return false;
				}
				ElevatorOvercon elevatorOvercon = OverconManager.Singleton.HighlightedOvercon as ElevatorOvercon;
				if (elevatorOvercon != null && elevatorOvercon != null)
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
				ElevatorChamber elevatorChamber;
				return this.ErrorCode == Scp079HudTranslation.Zoom && ElevatorChamber.TryGetChamber(this._lastElevator.Group, out elevatorChamber) && elevatorChamber.IsReady;
			}
		}

		public override string FailMessage
		{
			get
			{
				Scp079HudTranslation failedReason = this._failedReason;
				if (failedReason == Scp079HudTranslation.Zoom)
				{
					return null;
				}
				if (failedReason != Scp079HudTranslation.NotEnoughAux)
				{
					return Translations.Get<Scp079HudTranslation>(this._failedReason);
				}
				return base.GetNoAuxMessage((float)this._cost);
			}
		}

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._abilityName, this._cost);
			}
		}

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

		protected override void Start()
		{
			base.Start();
			this._abilityName = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.SendElevator);
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
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(elevatorGroup, out elevatorChamber))
			{
				return;
			}
			if (!elevatorChamber.IsReadyForUserInput)
			{
				return;
			}
			List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(elevatorGroup);
			if (doorsForGroup.All((ElevatorDoor x) => x.ActiveLocks > 0, true))
			{
				return;
			}
			RoomIdentifier curRoom = base.CurrentCamSync.CurrentCamera.Room;
			ElevatorDoor elevatorDoor;
			if (!doorsForGroup.TryGetFirst((ElevatorDoor x) => x.Rooms.Contains(curRoom), out elevatorDoor))
			{
				return;
			}
			bool targetState = elevatorDoor.TargetState;
			elevatorChamber.ServerSetDestination(elevatorChamber.NextLevel, false);
			base.AuxManager.CurrentAux -= (float)this._cost;
			doorsForGroup.ForEach(delegate(ElevatorDoor x)
			{
				this.RewardManager.MarkRooms(x.Rooms);
			});
			base.ServerSendRpc(false);
			if (targetState)
			{
				Action<Scp079Role, ElevatorDoor> onServerElevatorDoorClosed = Scp079ElevatorStateChanger.OnServerElevatorDoorClosed;
				if (onServerElevatorDoorClosed == null)
				{
					return;
				}
				onServerElevatorDoorClosed(base.CastRole, elevatorDoor);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			AudioSourcePoolManager.Play2D(this._confirmationSound, 1f, MixerChannel.DefaultSfx, 1f);
		}

		public override void OnFailMessageAssigned()
		{
			this._failedReason = this.ErrorCode;
		}

		[SerializeField]
		private AudioClip _confirmationSound;

		[SerializeField]
		private int _cost;

		private ElevatorDoor _lastElevator;

		private Scp079HudTranslation _failedReason;

		private string _abilityName;
	}
}
