using System;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079DoorStateChanger : Scp079DoorAbility
{
	[Serializable]
	private struct DoorCost
	{
		public DoorPermissionFlags Perm;

		public int Cost;
	}

	[SerializeField]
	private int _defaultCost;

	[SerializeField]
	private DoorCost[] _doorCostsheet;

	[SerializeField]
	private AudioClip _confirmationSound;

	private Scp079DoorLockChanger _lockChanger;

	private static string _openText;

	private static string _closeText;

	public override ActionName ActivationKey => ActionName.Shoot;

	public override string AbilityName => string.Format(base.LastDoor.TargetState ? Scp079DoorStateChanger._closeText : Scp079DoorStateChanger._openText, this.GetCostForDoor(this.TargetAction, base.LastDoor));

	protected override DoorAction TargetAction
	{
		get
		{
			if (!base.LastDoor.TargetState)
			{
				return DoorAction.Opened;
			}
			return DoorAction.Closed;
		}
	}

	public static event Action<Scp079Role, DoorVariant> OnServerDoorToggled;

	protected override void Start()
	{
		base.Start();
		Scp079DoorStateChanger._openText = Translations.Get(Scp079HudTranslation.OpenDoor);
		Scp079DoorStateChanger._closeText = Translations.Get(Scp079HudTranslation.CloseDoor);
		base.GetSubroutine<Scp079DoorLockChanger>(out this._lockChanger);
	}

	protected override int GetCostForDoor(DoorAction action, DoorVariant door)
	{
		DoorPermissionFlags requiredPermissions = door.RequiredPermissions.RequiredPermissions;
		int num = this._defaultCost;
		DoorCost[] doorCostsheet = this._doorCostsheet;
		for (int i = 0; i < doorCostsheet.Length; i++)
		{
			DoorCost doorCost = doorCostsheet[i];
			if (requiredPermissions.HasFlagAll(doorCost.Perm))
			{
				num = Mathf.Max(num, doorCost.Cost);
			}
		}
		if (this._lockChanger.LockedDoor == door && action == DoorAction.Closed)
		{
			num += this._lockChanger.LockClosedDoorCost - this._lockChanger.LockOpenDoorCost;
		}
		return num;
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteUInt(base.LastDoor.netId);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (NetworkServer.spawned.TryGetValue(reader.ReadUInt(), out var value) && value.TryGetComponent<DoorVariant>(out base.LastDoor) && this.IsReady && base.Role.TryGetOwner(out var hub) && !base.LostSignalHandler.Lost)
		{
			bool targetState = base.LastDoor.TargetState;
			base.LastDoor.ServerInteract(hub, 0);
			if (targetState != base.LastDoor.TargetState)
			{
				base.RewardManager.MarkRooms(base.LastDoor.Rooms);
				Scp079DoorStateChanger.OnServerDoorToggled?.Invoke(base.CastRole, base.LastDoor);
				base.AuxManager.CurrentAux -= this.GetCostForDoor(this.TargetAction, base.LastDoor);
				base.ServerSendRpc(toAll: true);
			}
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		base.PlayConfirmationSound(this._confirmationSound);
	}
}
