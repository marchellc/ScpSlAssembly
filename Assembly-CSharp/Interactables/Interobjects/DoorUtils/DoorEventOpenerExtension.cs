using System;
using GameCore;
using MapGeneration;
using Mirror;

namespace Interactables.Interobjects.DoorUtils;

public class DoorEventOpenerExtension : DoorVariantExtension
{
	public enum OpenerEventType
	{
		WarheadStart,
		WarheadCancel,
		DeconEvac,
		DeconFinish,
		DeconReset
	}

	private static bool _isolateCheckpoints;

	private static bool _configLoaded;

	public static event Action<OpenerEventType> OnDoorsTriggerred;

	public static void TriggerAction(OpenerEventType eventType)
	{
		DoorEventOpenerExtension.OnDoorsTriggerred(eventType);
	}

	private void Start()
	{
		DoorEventOpenerExtension.OnDoorsTriggerred += Trigger;
		DoorEventOpenerExtension._configLoaded = false;
	}

	private void OnDestroy()
	{
		DoorEventOpenerExtension.OnDoorsTriggerred -= Trigger;
	}

	private void Trigger(OpenerEventType eventType)
	{
		if (!NetworkServer.active || ((DoorLockReason)base.TargetDoor.ActiveLocks).HasFlagFast(DoorLockReason.SpecialDoorFeature))
		{
			return;
		}
		if (!DoorEventOpenerExtension._configLoaded)
		{
			DoorEventOpenerExtension._configLoaded = true;
			DoorEventOpenerExtension._isolateCheckpoints = ConfigFile.ServerConfig.GetBool("isolate_zones_on_countdown");
		}
		bool flag = base.transform.position.GetZone() == FacilityZone.LightContainment;
		switch (eventType)
		{
		case OpenerEventType.WarheadStart:
		{
			DoorNametagExtension component;
			if (DoorEventOpenerExtension._isolateCheckpoints && base.TargetDoor is CheckpointDoor checkpointDoor)
			{
				checkpointDoor.ServerChangeLock(DoorLockReason.Isolation, newState: true);
			}
			else if (AlphaWarheadController.LockGatesOnCountdown || !(base.TargetDoor is PryableDoor) || !base.TryGetComponent<DoorNametagExtension>(out component) || !component.GetName.Contains("GATE"))
			{
				base.TargetDoor.NetworkTargetState = true;
				base.TargetDoor.ServerChangeLock(DoorLockReason.Warhead, newState: true);
			}
			break;
		}
		case OpenerEventType.WarheadCancel:
			base.TargetDoor.ServerChangeLock(DoorLockReason.Warhead, newState: false);
			base.TargetDoor.ServerChangeLock(DoorLockReason.Isolation, newState: false);
			break;
		case OpenerEventType.DeconEvac:
			if (flag)
			{
				base.TargetDoor.NetworkTargetState = true;
				if (base.TargetDoor is CheckpointDoor)
				{
					base.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, newState: true);
				}
			}
			break;
		case OpenerEventType.DeconFinish:
			if (flag)
			{
				base.TargetDoor.NetworkTargetState = false;
				base.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, newState: false);
				base.TargetDoor.ServerChangeLock(DoorLockReason.DecontLockdown, newState: true);
			}
			break;
		case OpenerEventType.DeconReset:
			if (flag)
			{
				base.TargetDoor.ServerChangeLock(DoorLockReason.DecontEvacuate, newState: false);
				base.TargetDoor.ServerChangeLock(DoorLockReason.DecontLockdown, newState: false);
			}
			break;
		}
	}

	static DoorEventOpenerExtension()
	{
		DoorEventOpenerExtension.OnDoorsTriggerred = delegate
		{
		};
	}
}
