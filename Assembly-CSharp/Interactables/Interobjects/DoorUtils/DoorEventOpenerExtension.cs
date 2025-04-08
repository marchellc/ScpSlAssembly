using System;
using GameCore;
using Mirror;

namespace Interactables.Interobjects.DoorUtils
{
	public class DoorEventOpenerExtension : DoorVariantExtension
	{
		public static event Action<DoorEventOpenerExtension.OpenerEventType> OnDoorsTriggerred;

		public static void TriggerAction(DoorEventOpenerExtension.OpenerEventType eventType)
		{
			DoorEventOpenerExtension.OnDoorsTriggerred(eventType);
		}

		private void Start()
		{
			DoorEventOpenerExtension.OnDoorsTriggerred += this.Trigger;
			DoorEventOpenerExtension._configLoaded = false;
		}

		private void OnDestroy()
		{
			DoorEventOpenerExtension.OnDoorsTriggerred -= this.Trigger;
		}

		private void Trigger(DoorEventOpenerExtension.OpenerEventType eventType)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (((DoorLockReason)this.TargetDoor.ActiveLocks).HasFlagFast(DoorLockReason.SpecialDoorFeature))
			{
				return;
			}
			if (!DoorEventOpenerExtension._configLoaded)
			{
				DoorEventOpenerExtension._configLoaded = true;
				DoorEventOpenerExtension._isolateCheckpoints = ConfigFile.ServerConfig.GetBool("isolate_zones_on_countdown", false);
			}
			bool flag = base.transform.position.y > -100f && base.transform.position.y < 100f;
			switch (eventType)
			{
			case DoorEventOpenerExtension.OpenerEventType.WarheadStart:
			{
				if (DoorEventOpenerExtension._isolateCheckpoints)
				{
					CheckpointDoor checkpointDoor = this.TargetDoor as CheckpointDoor;
					if (checkpointDoor != null)
					{
						checkpointDoor.ServerChangeLock(DoorLockReason.Isolation, true);
						return;
					}
				}
				DoorNametagExtension doorNametagExtension;
				if (AlphaWarheadController.LockGatesOnCountdown || !(this.TargetDoor is PryableDoor) || !base.TryGetComponent<DoorNametagExtension>(out doorNametagExtension) || !doorNametagExtension.GetName.Contains("GATE"))
				{
					this.TargetDoor.NetworkTargetState = true;
					this.TargetDoor.ServerChangeLock(DoorLockReason.Warhead, true);
					return;
				}
				break;
			}
			case DoorEventOpenerExtension.OpenerEventType.WarheadCancel:
				this.TargetDoor.ServerChangeLock(DoorLockReason.Warhead, false);
				this.TargetDoor.ServerChangeLock(DoorLockReason.Isolation, false);
				return;
			case DoorEventOpenerExtension.OpenerEventType.DeconEvac:
				if (flag)
				{
					this.TargetDoor.NetworkTargetState = true;
					if (this.TargetDoor is CheckpointDoor)
					{
						this.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, true);
						return;
					}
				}
				break;
			case DoorEventOpenerExtension.OpenerEventType.DeconFinish:
				if (flag)
				{
					this.TargetDoor.NetworkTargetState = false;
					this.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, false);
					this.TargetDoor.ServerChangeLock(DoorLockReason.DecontLockdown, true);
					return;
				}
				break;
			case DoorEventOpenerExtension.OpenerEventType.DeconReset:
				if (flag)
				{
					this.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, false);
					this.TargetDoor.ServerChangeLock(DoorLockReason.DecontLockdown, false);
				}
				break;
			default:
				return;
			}
		}

		// Note: this type is marked as 'beforefieldinit'.
		static DoorEventOpenerExtension()
		{
			DoorEventOpenerExtension.OnDoorsTriggerred = delegate(DoorEventOpenerExtension.OpenerEventType eventType)
			{
			};
		}

		private static bool _isolateCheckpoints;

		private static bool _configLoaded;

		public enum OpenerEventType
		{
			WarheadStart,
			WarheadCancel,
			DeconEvac,
			DeconFinish,
			DeconReset
		}
	}
}
