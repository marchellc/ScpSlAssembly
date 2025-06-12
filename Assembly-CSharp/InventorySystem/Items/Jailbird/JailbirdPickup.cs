using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Jailbird;

public class JailbirdPickup : CollisionDetectionPickup, IUpgradeTrigger
{
	[SyncVar]
	public JailbirdWearState Wear;

	[SerializeField]
	private JailbirdMaterialController _materialController;

	private JailbirdWearState _prevWear;

	public float TotalMelee { get; set; }

	public int TotalCharges { get; set; }

	public JailbirdWearState NetworkWear
	{
		get
		{
			return this.Wear;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Wear, 2uL, null);
		}
	}

	public void ServerOnUpgraded(Scp914KnobSetting setting)
	{
		if (NetworkServer.active)
		{
			if (setting == Scp914KnobSetting.Coarse)
			{
				this.TotalCharges = JailbirdDeteriorationTracker.Scp914CoarseCharges;
				this.TotalMelee = JailbirdDeteriorationTracker.Scp914CoarseDamage;
				this.NetworkWear = JailbirdWearState.AlmostBroken;
			}
			else
			{
				this.TotalCharges = 0;
				this.TotalMelee = 0f;
				this.NetworkWear = JailbirdWearState.Healthy;
			}
		}
	}

	private void Update()
	{
		if (this._prevWear != this.Wear)
		{
			this.UpdateWearFromSyncvar();
		}
	}

	private void UpdateWearFromSyncvar()
	{
		this._prevWear = this.Wear;
		JailbirdDeteriorationTracker.ReceivedStates[base.Info.Serial] = this.Wear;
	}

	protected override void Start()
	{
		base.Start();
		this.UpdateWearFromSyncvar();
		this._materialController.SetSerial(base.Info.Serial);
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
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EJailbird_002EJailbirdWearState(writer, this.Wear);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			GeneratedNetworkCode._Write_InventorySystem_002EItems_002EJailbird_002EJailbirdWearState(writer, this.Wear);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Wear, null, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EJailbird_002EJailbirdWearState(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Wear, null, GeneratedNetworkCode._Read_InventorySystem_002EItems_002EJailbird_002EJailbirdWearState(reader));
		}
	}
}
