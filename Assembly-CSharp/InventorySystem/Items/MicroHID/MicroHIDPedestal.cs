using System;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using UnityEngine;

namespace InventorySystem.Items.MicroHID;

public class MicroHIDPedestal : Locker
{
	private Transform _trackedPickup;

	private bool _isTrackingPickup;

	private const float DisconnectPosOffsetSqr = 0.0001f;

	private const float DisconnectRotationDot = 0.99f;

	protected override void ServerFillChambers()
	{
		if (Chambers.Length != 1)
		{
			throw new InvalidOperationException("MicroHID pedestal can only have 1 chamber.");
		}
		LockerChamber lockerChamber = Chambers[0];
		lockerChamber.SpawnItem(ItemType.MicroHID, 1);
		if (TryGetSpawnedMicro(lockerChamber, out var pickup))
		{
			pickup.OnSelfDestroyed += ReleaseConnectionWithPickup;
			_isTrackingPickup = true;
			_trackedPickup = pickup.transform;
			PickupSyncInfo info = pickup.Info;
			info.Locked = false;
			pickup.NetworkInfo = info;
			if (info.ItemId.TryGetTemplate<MicroHIDItem>(out var item) && item.TryGetSubcomponent<DrawAndInspectorModule>(out var ret))
			{
				ret.ServerRegisterSerial(info.Serial);
			}
		}
	}

	protected override bool CheckTogglePerms(int chamberId, ReferenceHub ply, out PermissionUsed callback)
	{
		callback = null;
		return false;
	}

	protected override void Update()
	{
		base.Update();
		if (_isTrackingPickup)
		{
			_trackedPickup.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
			if (!(localPosition.sqrMagnitude < 0.0001f) || !(localRotation.w > 0.99f))
			{
				ReleaseConnectionWithPickup();
			}
		}
	}

	private void ReleaseConnectionWithPickup()
	{
		base.NetworkOpenedChambers = 1;
		_isTrackingPickup = false;
	}

	private bool TryGetSpawnedMicro(LockerChamber chamber, out MicroHIDPickup pickup)
	{
		foreach (ItemPickupBase item in chamber.Content)
		{
			if (!(item == null) && item is MicroHIDPickup microHIDPickup)
			{
				pickup = microHIDPickup;
				return true;
			}
		}
		pickup = null;
		return false;
	}

	public override bool Weaved()
	{
		return true;
	}
}
