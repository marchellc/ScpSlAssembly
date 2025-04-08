using System;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079DoorStateChanger : Scp079DoorAbility
	{
		public static event Action<Scp079Role, DoorVariant> OnServerDoorToggled;

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
				return string.Format(this.LastDoor.TargetState ? Scp079DoorStateChanger._closeText : Scp079DoorStateChanger._openText, this.GetCostForDoor(this.TargetAction, this.LastDoor));
			}
		}

		protected override DoorAction TargetAction
		{
			get
			{
				if (!this.LastDoor.TargetState)
				{
					return DoorAction.Opened;
				}
				return DoorAction.Closed;
			}
		}

		protected override void Start()
		{
			base.Start();
			Scp079DoorStateChanger._openText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.OpenDoor);
			Scp079DoorStateChanger._closeText = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.CloseDoor);
			base.GetSubroutine<Scp079DoorLockChanger>(out this._lockChanger);
		}

		protected override int GetCostForDoor(DoorAction action, DoorVariant door)
		{
			KeycardPermissions requiredPermissions = door.RequiredPermissions.RequiredPermissions;
			int num = this._defaultCost;
			foreach (Scp079DoorStateChanger.DoorCost doorCost in this._doorCostsheet)
			{
				if (requiredPermissions.HasFlagFast(doorCost.Perm))
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
			writer.WriteUInt(this.LastDoor.netId);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			NetworkIdentity networkIdentity;
			if (!NetworkServer.spawned.TryGetValue(reader.ReadUInt(), out networkIdentity))
			{
				return;
			}
			if (!networkIdentity.TryGetComponent<DoorVariant>(out this.LastDoor))
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!this.IsReady || !base.Role.TryGetOwner(out referenceHub) || base.LostSignalHandler.Lost)
			{
				return;
			}
			bool targetState = this.LastDoor.TargetState;
			this.LastDoor.ServerInteract(referenceHub, 0);
			if (targetState == this.LastDoor.TargetState)
			{
				return;
			}
			base.RewardManager.MarkRooms(this.LastDoor.Rooms);
			Action<Scp079Role, DoorVariant> onServerDoorToggled = Scp079DoorStateChanger.OnServerDoorToggled;
			if (onServerDoorToggled != null)
			{
				onServerDoorToggled(base.CastRole, this.LastDoor);
			}
			base.AuxManager.CurrentAux -= (float)this.GetCostForDoor(this.TargetAction, this.LastDoor);
			base.ServerSendRpc(true);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			base.PlayConfirmationSound(this._confirmationSound);
		}

		[SerializeField]
		private int _defaultCost;

		[SerializeField]
		private Scp079DoorStateChanger.DoorCost[] _doorCostsheet;

		[SerializeField]
		private AudioClip _confirmationSound;

		private Scp079DoorLockChanger _lockChanger;

		private static string _openText;

		private static string _closeText;

		[Serializable]
		private struct DoorCost
		{
			public KeycardPermissions Perm;

			public int Cost;
		}
	}
}
