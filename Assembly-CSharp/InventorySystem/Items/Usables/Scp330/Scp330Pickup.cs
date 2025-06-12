using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330;

public class Scp330Pickup : CollisionDetectionPickup
{
	[Serializable]
	private struct IndividualCandy
	{
		[SerializeField]
		private CandyKindID _kind;

		[SerializeField]
		private GameObject _candyObject;

		public void Refresh(CandyKindID exposed)
		{
			this._candyObject.SetActive(exposed == this._kind);
		}
	}

	public List<CandyKindID> StoredCandies = new List<CandyKindID>();

	[SyncVar]
	public CandyKindID ExposedCandy;

	[SerializeField]
	private IndividualCandy[] _candyTypes;

	private int _prevExposed = -1;

	public CandyKindID NetworkExposedCandy
	{
		get
		{
			return this.ExposedCandy;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ExposedCandy, 2uL, null);
		}
	}

	public override PickupSearchCompletor GetPickupSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		return new Scp330SearchCompletor(coordinator.Hub, this, sqrDistance);
	}

	private void Update()
	{
		int exposedCandy = (int)this.ExposedCandy;
		if (this._prevExposed != exposedCandy)
		{
			IndividualCandy[] candyTypes = this._candyTypes;
			foreach (IndividualCandy individualCandy in candyTypes)
			{
				individualCandy.Refresh(this.ExposedCandy);
			}
			this._prevExposed = exposedCandy;
			if (NetworkServer.active && this.StoredCandies.Count == 0)
			{
				base.DestroySelf();
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EUsables_002EScp330_002ECandyKindID(writer, this.ExposedCandy);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EUsables_002EScp330_002ECandyKindID(writer, this.ExposedCandy);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.ExposedCandy, null, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EUsables_002EScp330_002ECandyKindID(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ExposedCandy, null, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EUsables_002EScp330_002ECandyKindID(reader));
		}
	}
}
