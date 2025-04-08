using System;
using GameObjectPools;
using InventorySystem;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Inventory : SubroutineBase, IInteractionBlocker, IPoolSpawnable
	{
		public BlockedInteraction BlockedInteractions
		{
			get
			{
				if (!(base.Role as Scp3114Role).Disguised)
				{
					return BlockedInteraction.OpenInventory | BlockedInteraction.GrabItems;
				}
				return BlockedInteraction.ItemPrimaryAction;
			}
		}

		public bool CanBeCleared
		{
			get
			{
				return !base.Role.IsLocalPlayer;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this._scpRole = base.Role as Scp3114Role;
			this._scpRole.CurIdentity.OnStatusChanged += this.OnStatusChanged;
		}

		private void OnStatusChanged()
		{
			ReferenceHub referenceHub;
			if (!NetworkServer.active || !base.Role.TryGetOwner(out referenceHub))
			{
				return;
			}
			referenceHub.inventory.ServerDropEverything();
		}

		public void SpawnObject()
		{
			ReferenceHub referenceHub;
			base.Role.TryGetOwner(out referenceHub);
			referenceHub.interCoordinator.AddBlocker(this);
		}

		private Scp3114Role _scpRole;

		private const BlockedInteraction DisguiseBlockers = BlockedInteraction.ItemPrimaryAction;

		private const BlockedInteraction SkeletonBlockers = BlockedInteraction.OpenInventory | BlockedInteraction.GrabItems;
	}
}
