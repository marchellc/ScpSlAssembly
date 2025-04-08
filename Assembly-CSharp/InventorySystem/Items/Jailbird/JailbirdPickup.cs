using System;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Jailbird
{
	public class JailbirdPickup : CollisionDetectionPickup, IUpgradeTrigger
	{
		public float TotalMelee { get; set; }

		public int TotalCharges { get; set; }

		public void ServerOnUpgraded(Scp914KnobSetting setting)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (setting == Scp914KnobSetting.Coarse)
			{
				this.TotalCharges = JailbirdDeteriorationTracker.Scp914CoarseCharges;
				this.TotalMelee = JailbirdDeteriorationTracker.Scp914CoarseDamage;
				this.NetworkWear = JailbirdWearState.AlmostBroken;
				return;
			}
			this.TotalCharges = 0;
			this.TotalMelee = 0f;
			this.NetworkWear = JailbirdWearState.Healthy;
		}

		private void Update()
		{
			if (this._prevWear == this.Wear)
			{
				return;
			}
			this.UpdateWearFromSyncvar();
		}

		private void UpdateWearFromSyncvar()
		{
			this._prevWear = this.Wear;
			JailbirdDeteriorationTracker.ReceivedStates[this.Info.Serial] = this.Wear;
		}

		protected override void Start()
		{
			base.Start();
			this.UpdateWearFromSyncvar();
			this._materialController.SetSerial(this.Info.Serial);
		}

		public override bool Weaved()
		{
			return true;
		}

		public JailbirdWearState NetworkWear
		{
			get
			{
				return this.Wear;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<JailbirdWearState>(value, ref this.Wear, 2UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.Jailbird.JailbirdWearState(writer, this.Wear);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_InventorySystem.Items.Jailbird.JailbirdWearState(writer, this.Wear);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<JailbirdWearState>(ref this.Wear, null, global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.Jailbird.JailbirdWearState(reader));
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<JailbirdWearState>(ref this.Wear, null, global::Mirror.GeneratedNetworkCode._Read_InventorySystem.Items.Jailbird.JailbirdWearState(reader));
			}
		}

		[SyncVar]
		public JailbirdWearState Wear;

		[SerializeField]
		private JailbirdMaterialController _materialController;

		private JailbirdWearState _prevWear;
	}
}
