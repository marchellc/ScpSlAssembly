using System;
using System.Collections.Generic;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID;

public class MicroHIDPickup : CollisionDetectionPickup
{
	public static readonly Dictionary<ushort, MicroHIDPickup> PickupsBySerial = new Dictionary<ushort, MicroHIDPickup>();

	[SerializeField]
	private MicroHIDParticles _particles;

	[SerializeField]
	private Transform _simulatedOwnerCam;

	private CycleController _cycleController;

	public event Action OnSelfDestroyed;

	private void Update()
	{
		if (NetworkServer.active)
		{
			this._cycleController.ServerUpdatePickup(this);
		}
	}

	protected override void Start()
	{
		base.Start();
		ushort serial = base.Info.Serial;
		MicroHIDPickup.PickupsBySerial[serial] = this;
		this._particles.Init(serial, this._simulatedOwnerCam);
		if (NetworkServer.active)
		{
			this._cycleController = CycleSyncModule.GetCycleController(serial);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		this.OnSelfDestroyed?.Invoke();
		MicroHIDPickup.PickupsBySerial.Remove(base.Info.Serial);
	}

	public override bool Weaved()
	{
		return true;
	}
}
