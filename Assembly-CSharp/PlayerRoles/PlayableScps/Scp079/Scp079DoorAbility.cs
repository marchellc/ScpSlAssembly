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

namespace PlayerRoles.PlayableScps.Scp079;

public abstract class Scp079DoorAbility : Scp079KeyAbilityBase
{
	protected DoorVariant LastDoor;

	private static string _deniedText;

	private int _lastCost;

	private bool _lastActionValid;

	private int _failMessageAux;

	private bool _failMessageDenied;

	public override bool IsVisible
	{
		get
		{
			if (Scp079CursorManager.LockCameras)
			{
				return false;
			}
			if (OverconManager.Singleton.HighlightedOvercon is DoorOvercon doorOvercon && doorOvercon != null)
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
			if (this._lastActionValid)
			{
				return (float)this._lastCost <= base.AuxManager.CurrentAux;
			}
			return false;
		}
	}

	public override string FailMessage
	{
		get
		{
			if (!this._failMessageDenied)
			{
				if (!(base.AuxManager.CurrentAux < (float)this._failMessageAux))
				{
					return null;
				}
				return base.GetNoAuxMessage(this._failMessageAux);
			}
			return Scp079DoorAbility._deniedText;
		}
	}

	protected abstract DoorAction TargetAction { get; }

	public static event Action<Scp079Role, DoorVariant> OnServerAnyDoorInteraction;

	protected abstract int GetCostForDoor(DoorAction action, DoorVariant door);

	protected override void Trigger()
	{
		base.ClientSendCmd();
	}

	protected override void Start()
	{
		base.Start();
		Scp079DoorAbility._deniedText = Translations.Get(Scp079HudTranslation.DoorAccessDenied);
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
		if (base.Role.IsLocalPlayer || base.Owner.IsLocallySpectated())
		{
			AudioSourcePoolManager.Play2D(sound);
		}
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
		if (door is IDamageableDoor { IsDestroyed: not false })
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
			if (mode != DoorLockMode.FullLock)
			{
				return !(door is CheckpointDoor);
			}
			return false;
		case DoorAction.Unlocked:
			return true;
		default:
			return false;
		}
	}

	public static bool CheckVisibility(DoorVariant door, Scp079Camera currentCamera)
	{
		RoomIdentifier[] rooms = door.Rooms;
		for (int i = 0; i < rooms.Length; i++)
		{
			if (!(rooms[i] != currentCamera.Room))
			{
				if (door is INonInteractableDoor nonInteractableDoor)
				{
					return !nonInteractableDoor.IgnoreLockdowns;
				}
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Scp079DoorLockChanger.OnServerDoorLocked += delegate(Scp079Role role, DoorVariant dv)
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction?.Invoke(role, dv);
		};
		Scp079DoorStateChanger.OnServerDoorToggled += delegate(Scp079Role role, DoorVariant dv)
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction?.Invoke(role, dv);
		};
		Scp079ElevatorStateChanger.OnServerElevatorDoorClosed += delegate(Scp079Role role, ElevatorDoor dv)
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction?.Invoke(role, dv);
		};
		Scp079LockdownRoomAbility.OnServerDoorLocked += delegate(Scp079Role role, DoorVariant dv)
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction?.Invoke(role, dv);
		};
	}
}
