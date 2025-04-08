using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330
{
	public class Scp330Pickup : CollisionDetectionPickup
	{
		private void Update()
		{
			int exposedCandy = (int)this.ExposedCandy;
			if (this._prevExposed == exposedCandy)
			{
				return;
			}
			foreach (Scp330Pickup.IndividualCandy individualCandy in this._candyTypes)
			{
				individualCandy.Refresh(this.ExposedCandy);
			}
			this._prevExposed = exposedCandy;
			if (NetworkServer.active && this.StoredCandies.Count == 0)
			{
				base.DestroySelf();
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public CandyKindID NetworkExposedCandy
		{
			get
			{
				return this.ExposedCandy;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<CandyKindID>(value, ref this.ExposedCandy, 2UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp330.CandyKindID(writer, this.ExposedCandy);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.Usables.Scp330.CandyKindID(writer, this.ExposedCandy);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<CandyKindID>(ref this.ExposedCandy, null, global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp330.CandyKindID(reader));
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<CandyKindID>(ref this.ExposedCandy, null, global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.Usables.Scp330.CandyKindID(reader));
			}
		}

		public List<CandyKindID> StoredCandies = new List<CandyKindID>();

		[SyncVar]
		public CandyKindID ExposedCandy;

		[SerializeField]
		private Scp330Pickup.IndividualCandy[] _candyTypes;

		private int _prevExposed = -1;

		[Serializable]
		private struct IndividualCandy
		{
			public void Refresh(CandyKindID exposed)
			{
				this._candyObject.SetActive(exposed == this._kind);
			}

			[SerializeField]
			private CandyKindID _kind;

			[SerializeField]
			private GameObject _candyObject;
		}
	}
}
