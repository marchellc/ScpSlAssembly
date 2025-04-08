using System;
using Footprinting;
using Interactables.Interobjects.DoorUtils;

namespace Interactables.Interobjects
{
	public class DummyDoor : DoorVariant, IDamageableDoor, INonInteractableDoor
	{
		public bool IsDestroyed
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		public float MaxHealth
		{
			get
			{
				return 0f;
			}
		}

		public float RemainingHealth
		{
			get
			{
				return 0f;
			}
		}

		public bool IgnoreLockdowns { get; private set; }

		public bool IgnoreRemoteAdmin { get; private set; }

		public override void LockBypassDenied(ReferenceHub ply, byte colliderId)
		{
		}

		public override void PermissionsDenied(ReferenceHub ply, byte colliderId)
		{
		}

		public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
		{
			return false;
		}

		public override bool AnticheatPassageApproved()
		{
			return false;
		}

		public override float GetExactState()
		{
			return (float)(this.TargetState ? 1 : 0);
		}

		public float GetHealthPercent()
		{
			return 0f;
		}

		public override bool IsConsideredOpen()
		{
			return this.TargetState;
		}

		public bool ServerDamage(float hp, DoorDamageType type, Footprint attacker = default(Footprint))
		{
			return false;
		}

		public bool ServerRepair()
		{
			return false;
		}

		public void ClientDestroyEffects()
		{
		}

		public void ClientRepairEffects()
		{
		}

		public override bool Weaved()
		{
			return true;
		}
	}
}
