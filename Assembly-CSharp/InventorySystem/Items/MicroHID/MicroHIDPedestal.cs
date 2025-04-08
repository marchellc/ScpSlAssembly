using System;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDPedestal : Locker
	{
		protected override void ServerFillChambers()
		{
			if (this.Chambers.Length != 1)
			{
				throw new InvalidOperationException("MicroHID pedestal can only have 1 chamber.");
			}
			LockerChamber lockerChamber = this.Chambers[0];
			lockerChamber.SpawnItem(ItemType.MicroHID, 1);
			MicroHIDPickup microHIDPickup;
			if (!this.TryGetSpawnedMicro(lockerChamber, out microHIDPickup))
			{
				return;
			}
			microHIDPickup.OnSelfDestroyed += this.ReleaseConnectionWithPickup;
			this._isTrackingPickup = true;
			this._trackedPickup = microHIDPickup.transform;
			PickupSyncInfo info = microHIDPickup.Info;
			info.Locked = false;
			microHIDPickup.NetworkInfo = info;
			MicroHIDItem microHIDItem;
			DrawAndInspectorModule drawAndInspectorModule;
			if (info.ItemId.TryGetTemplate(out microHIDItem) && microHIDItem.TryGetSubcomponent<DrawAndInspectorModule>(out drawAndInspectorModule))
			{
				drawAndInspectorModule.ServerRegisterSerial(info.Serial);
			}
		}

		protected override bool CheckTogglePerms(int chamberId, ReferenceHub ply)
		{
			return false;
		}

		protected override void Update()
		{
			base.Update();
			if (!this._isTrackingPickup)
			{
				return;
			}
			Vector3 vector;
			Quaternion quaternion;
			this._trackedPickup.GetLocalPositionAndRotation(out vector, out quaternion);
			if (vector.sqrMagnitude < 0.0001f && quaternion.w > 0.99f)
			{
				return;
			}
			this.ReleaseConnectionWithPickup();
		}

		private void ReleaseConnectionWithPickup()
		{
			base.NetworkOpenedChambers = 1;
			this._isTrackingPickup = false;
		}

		private bool TryGetSpawnedMicro(LockerChamber chamber, out MicroHIDPickup pickup)
		{
			foreach (ItemPickupBase itemPickupBase in chamber.Content)
			{
				if (!(itemPickupBase == null))
				{
					MicroHIDPickup microHIDPickup = itemPickupBase as MicroHIDPickup;
					if (microHIDPickup != null)
					{
						pickup = microHIDPickup;
						return true;
					}
				}
			}
			pickup = null;
			return false;
		}

		public override bool Weaved()
		{
			return true;
		}

		private Transform _trackedPickup;

		private bool _isTrackingPickup;

		private const float DisconnectPosOffsetSqr = 0.0001f;

		private const float DisconnectRotationDot = 0.99f;
	}
}
