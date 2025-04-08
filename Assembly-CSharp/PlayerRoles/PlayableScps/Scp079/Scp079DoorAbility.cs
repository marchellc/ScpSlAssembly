using System;
using AudioPooling;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public abstract class Scp079DoorAbility : Scp079KeyAbilityBase
	{
		public static event Action<Scp079Role, DoorVariant> OnServerAnyDoorInteraction;

		public override bool IsVisible
		{
			get
			{
				if (Scp079CursorManager.LockCameras)
				{
					return false;
				}
				DoorOvercon doorOvercon = OverconManager.Singleton.HighlightedOvercon as DoorOvercon;
				if (doorOvercon != null && doorOvercon != null)
				{
					this.LastDoor = doorOvercon.Target;
					return true;
				}
				return false;
			}
		}

		public override bool IsReady
		{
			get
			{
				DoorAction targetAction = this.TargetAction;
				this._lastActionValid = Scp079DoorAbility.ValidateAction(targetAction, this.LastDoor, base.CurrentCamSync.CurrentCamera);
				this._lastCost = this.GetCostForDoor(targetAction, this.LastDoor);
				return this._lastActionValid && (float)this._lastCost <= base.AuxManager.CurrentAux;
			}
		}

		public override string FailMessage
		{
			get
			{
				if (this._failMessageDenied)
				{
					return Scp079DoorAbility._deniedText;
				}
				if (base.AuxManager.CurrentAux >= (float)this._failMessageAux)
				{
					return null;
				}
				return base.GetNoAuxMessage((float)this._failMessageAux);
			}
		}

		protected abstract DoorAction TargetAction { get; }

		protected abstract int GetCostForDoor(DoorAction action, DoorVariant door);

		protected override void Trigger()
		{
			base.ClientSendCmd();
		}

		protected override void Start()
		{
			base.Start();
			Scp079DoorAbility._deniedText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.DoorAccessDenied);
			base.CurrentCamSync.OnCameraChanged += delegate
			{
				this._failMessageAux = 0;
				this._failMessageDenied = false;
			};
		}

		public override void OnFailMessageAssigned()
		{
			this._failMessageDenied = !this._lastActionValid;
			this._failMessageAux = this._lastCost;
		}

		public void PlayConfirmationSound(AudioClip sound)
		{
			if (!base.Role.IsLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return;
			}
			AudioSourcePoolManager.Play2D(sound, 1f, MixerChannel.DefaultSfx, 1f);
		}

		public static bool ValidateAction(DoorAction action, DoorVariant door, Scp079Camera currentCamera)
		{
			if (!Scp079DoorAbility.CheckVisibility(door, currentCamera))
			{
				return false;
			}
			DoorLockReason activeLocks = (DoorLockReason)door.ActiveLocks;
			if (activeLocks.HasFlagFast(DoorLockReason.Warhead) || activeLocks.HasFlagFast(DoorLockReason.Isolation))
			{
				return false;
			}
			DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)door.ActiveLocks);
			IDamageableDoor damageableDoor = door as IDamageableDoor;
			if (damageableDoor != null && damageableDoor.IsDestroyed)
			{
				return false;
			}
			if (mode.HasFlagFast(DoorLockMode.ScpOverride))
			{
				return true;
			}
			switch (action)
			{
			case DoorAction.Opened:
				return mode.HasFlagFast(DoorLockMode.CanOpen);
			case DoorAction.Closed:
				return mode.HasFlagFast(DoorLockMode.CanClose);
			case DoorAction.Locked:
				return mode != DoorLockMode.FullLock && !(door is CheckpointDoor);
			case DoorAction.Unlocked:
				return true;
			}
			return false;
		}

		public static bool CheckVisibility(DoorVariant door, Scp079Camera currentCamera)
		{
			RoomIdentifier[] rooms = door.Rooms;
			for (int i = 0; i < rooms.Length; i++)
			{
				if (!(rooms[i] != currentCamera.Room))
				{
					INonInteractableDoor nonInteractableDoor = door as INonInteractableDoor;
					return nonInteractableDoor == null || !nonInteractableDoor.IgnoreLockdowns;
				}
			}
			return false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Scp079DoorLockChanger.OnServerDoorLocked += delegate(Scp079Role role, DoorVariant dv)
			{
				Action<Scp079Role, DoorVariant> onServerAnyDoorInteraction = Scp079DoorAbility.OnServerAnyDoorInteraction;
				if (onServerAnyDoorInteraction == null)
				{
					return;
				}
				onServerAnyDoorInteraction(role, dv);
			};
			Scp079DoorStateChanger.OnServerDoorToggled += delegate(Scp079Role role, DoorVariant dv)
			{
				Action<Scp079Role, DoorVariant> onServerAnyDoorInteraction2 = Scp079DoorAbility.OnServerAnyDoorInteraction;
				if (onServerAnyDoorInteraction2 == null)
				{
					return;
				}
				onServerAnyDoorInteraction2(role, dv);
			};
			Scp079ElevatorStateChanger.OnServerElevatorDoorClosed += delegate(Scp079Role role, ElevatorDoor dv)
			{
				Action<Scp079Role, DoorVariant> onServerAnyDoorInteraction3 = Scp079DoorAbility.OnServerAnyDoorInteraction;
				if (onServerAnyDoorInteraction3 == null)
				{
					return;
				}
				onServerAnyDoorInteraction3(role, dv);
			};
			Scp079LockdownRoomAbility.OnServerDoorLocked += delegate(Scp079Role role, DoorVariant dv)
			{
				Action<Scp079Role, DoorVariant> onServerAnyDoorInteraction4 = Scp079DoorAbility.OnServerAnyDoorInteraction;
				if (onServerAnyDoorInteraction4 == null)
				{
					return;
				}
				onServerAnyDoorInteraction4(role, dv);
			};
		}

		protected DoorVariant LastDoor;

		private static string _deniedText;

		private int _lastCost;

		private bool _lastActionValid;

		private int _failMessageAux;

		private bool _failMessageDenied;
	}
}
