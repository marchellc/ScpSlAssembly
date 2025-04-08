using System;
using System.Collections.Generic;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDPickup : CollisionDetectionPickup
	{
		public event Action OnSelfDestroyed;

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._cycleController.ServerUpdatePickup(this);
		}

		protected override void Start()
		{
			base.Start();
			ushort serial = this.Info.Serial;
			MicroHIDPickup.PickupsBySerial[serial] = this;
			this._particles.Init(serial, this._simulatedOwnerCam);
			if (!NetworkServer.active)
			{
				return;
			}
			this._cycleController = CycleSyncModule.GetCycleController(serial);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Action onSelfDestroyed = this.OnSelfDestroyed;
			if (onSelfDestroyed != null)
			{
				onSelfDestroyed();
			}
			MicroHIDPickup.PickupsBySerial.Remove(this.Info.Serial);
		}

		public override bool Weaved()
		{
			return true;
		}

		public static readonly Dictionary<ushort, MicroHIDPickup> PickupsBySerial = new Dictionary<ushort, MicroHIDPickup>();

		[SerializeField]
		private MicroHIDParticles _particles;

		[SerializeField]
		private Transform _simulatedOwnerCam;

		private CycleController _cycleController;
	}
}
