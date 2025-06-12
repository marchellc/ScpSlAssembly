using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class MagazineModule : ModuleBase, IReloadUnloadValidatorModule, IMagazineControllerModule, IPrimaryAmmoContainerModule, IAmmoContainerModule, IDisplayableAmmoProviderModule, IAmmoDropPreventer
{
	private static readonly Dictionary<ushort, int> SyncData = new Dictionary<ushort, int>();

	[SerializeField]
	private int _defaultCapacity;

	[SerializeField]
	private ItemType _ammoType;

	[SerializeField]
	private AnimatorConditionalOverride _magazineRemovedOverrideLayers;

	public ItemType AmmoType => this._ammoType;

	public virtual IReloadUnloadValidatorModule.Authorization ReloadAuthorization
	{
		get
		{
			if (this.AmmoStored >= this.AmmoMax || base.Firearm.OwnerInventory.GetCurAmmo(this.AmmoType) <= 0)
			{
				return IReloadUnloadValidatorModule.Authorization.Idle;
			}
			return IReloadUnloadValidatorModule.Authorization.Allowed;
		}
	}

	public virtual IReloadUnloadValidatorModule.Authorization UnloadAuthorization
	{
		get
		{
			if (this.AmmoStored <= 0)
			{
				return IReloadUnloadValidatorModule.Authorization.Idle;
			}
			return IReloadUnloadValidatorModule.Authorization.Allowed;
		}
	}

	public int AmmoMax => this._defaultCapacity + (int)base.Firearm.AttachmentsValue(AttachmentParam.MagazineCapacityModifier);

	public int AmmoStored
	{
		get
		{
			return this.GetAmmoStoredForSerial(base.ItemSerial);
		}
		private set
		{
			if (this.MagazineInserted)
			{
				MagazineModule.SyncData[base.ItemSerial] = value + 1;
			}
		}
	}

	public bool MagazineInserted
	{
		get
		{
			return MagazineModule.GetMagazineInserted(base.ItemSerial);
		}
		private set
		{
			MagazineModule.SyncData[base.ItemSerial] = (value ? (this.AmmoStored + 1) : 0);
		}
	}

	public DisplayAmmoValues PredictedDisplayAmmo => default(DisplayAmmoValues);

	private Inventory UserInv => base.Firearm.OwnerInventory;

	public static event Action<ushort> OnDataReceived;

	public static bool GetMagazineInserted(ushort serial)
	{
		if (MagazineModule.SyncData.TryGetValue(serial, out var value))
		{
			return value > 0;
		}
		return false;
	}

	public void ServerSetInstanceAmmo(ushort serial, int amount)
	{
		MagazineModule.SyncData[serial] = amount + 1;
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteUShort(serial);
			writer.WriteByte((byte)(amount + 1));
		});
	}

	[ExposedFirearmEvent]
	public void ServerRemoveMagazine()
	{
		if (base.IsServer)
		{
			this.UserInv.ServerAddAmmo(this.AmmoType, this.AmmoStored);
			this.MagazineInserted = false;
			this.ServerResyncData();
		}
	}

	[ExposedFirearmEvent]
	public void ServerInsertMagazine()
	{
		if (base.IsServer)
		{
			this.ServerInsertEmptyMagazine();
			this.ServerLoadAmmoFromInventory();
		}
	}

	[ExposedFirearmEvent]
	public void ServerInsertEmptyMagazine()
	{
		if (base.IsServer)
		{
			this.MagazineInserted = true;
			this.ServerResyncData();
		}
	}

	[ExposedFirearmEvent]
	public void ServerLoadAmmoFromInventory(int insertionLimit)
	{
		if (base.IsServer)
		{
			int b = this.AmmoMax - this.AmmoStored;
			int num = Mathf.Min(Mathf.Min(this.UserInv.GetCurAmmo(this.AmmoType), b), insertionLimit);
			this.UserInv.ServerAddAmmo(this.AmmoType, -num);
			this.AmmoStored += num;
			this.ServerResyncData();
		}
	}

	[ExposedFirearmEvent]
	public void ServerLoadAmmoFromInventory()
	{
		this.ServerLoadAmmoFromInventory(int.MaxValue);
	}

	public void ServerResyncData()
	{
		if (MagazineModule.SyncData.TryGetValue(base.ItemSerial, out var syncData))
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteUShort(base.ItemSerial);
				writer.WriteByte((byte)syncData);
			});
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		while (reader.Remaining > 0)
		{
			ushort num = reader.ReadUShort();
			byte value = reader.ReadByte();
			MagazineModule.SyncData[num] = value;
			MagazineModule.OnDataReceived?.Invoke(num);
		}
	}

	public void ServerModifyAmmo(int amt)
	{
		this.AmmoStored += amt;
		this.ServerResyncData();
	}

	public int GetAmmoStoredForSerial(ushort serial)
	{
		if (!MagazineModule.SyncData.TryGetValue(serial, out var value))
		{
			return 0;
		}
		return Mathf.Max(0, value - 1);
	}

	public DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial)
	{
		return new DisplayAmmoValues(this.GetAmmoStoredForSerial(serial));
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
	{
		base.ServerOnPlayerConnected(hub, firstModule);
		if (!firstModule)
		{
			return;
		}
		this.SendRpc(hub, delegate(NetworkWriter writer)
		{
			foreach (KeyValuePair<ushort, int> syncDatum in MagazineModule.SyncData)
			{
				writer.WriteUShort(syncDatum.Key);
				writer.WriteByte((byte)syncDatum.Value);
			}
		});
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		this._magazineRemovedOverrideLayers.Update(base.Firearm, !this.MagazineInserted);
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.MagazineAmmo, this.AmmoStored, checkIfExists: true);
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsMagInserted, this.MagazineInserted, checkIfExists: true);
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.IsServer)
		{
			switch (base.Firearm.ServerAddReason)
			{
			case ItemAddReason.AdminCommand:
			case ItemAddReason.Scp2536:
				this.MagazineInserted = true;
				this.AmmoStored = this.AmmoMax;
				break;
			case ItemAddReason.StartingItem:
				this.ServerInsertMagazine();
				return;
			}
			this.ServerResyncData();
		}
	}

	internal override void OnAttachmentsApplied()
	{
		base.OnAttachmentsApplied();
		if (base.IsServer)
		{
			int num = this.AmmoStored - this.AmmoMax;
			if (num > 0)
			{
				this.AmmoStored -= num;
				this.UserInv.ServerAddAmmo(this.AmmoType, num);
				this.ServerResyncData();
			}
		}
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (base.IsServer)
		{
			this.ServerResyncData();
		}
	}

	internal override void ServerProcessMapgenDistribution(ItemPickupBase pickupBase)
	{
		base.ServerProcessMapgenDistribution(pickupBase);
		if (pickupBase is FirearmPickup firearmPickup && AttachmentPreview.TryGet(firearmPickup.CurId, reValidate: false, out var result) && result.TryGetModule<MagazineModule>(out var module))
		{
			MagazineModule.SyncData[firearmPickup.CurId.SerialNumber] = module.AmmoMax + 1;
		}
	}

	internal override void ServerProcessScp914Creation(ushort serial, Scp914KnobSetting knobSetting, Scp914Result scp914Result, ItemType itemType)
	{
		base.ServerProcessScp914Creation(serial, knobSetting, scp914Result, itemType);
		if (itemType != ItemType.ParticleDisruptor)
		{
			return;
		}
		MagazineModule.SyncData[serial] = this.AmmoMax + 1;
		if (MagazineModule.SyncData.TryGetValue(serial, out var syncData))
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteUShort(serial);
				writer.WriteByte((byte)syncData);
			});
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		MagazineModule.SyncData.Clear();
	}

	public bool ValidateAmmoDrop(ItemType id)
	{
		if (id == this.AmmoType && base.Firearm.TryGetModule<IReloaderModule>(out var module))
		{
			return !module.IsReloadingOrUnloading;
		}
		return true;
	}
}
