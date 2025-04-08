using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class MagazineModule : ModuleBase, IReloadUnloadValidatorModule, IMagazineControllerModule, IPrimaryAmmoContainerModule, IAmmoContainerModule, IDisplayableAmmoProviderModule, IAmmoDropPreventer
	{
		public static event Action<ushort> OnDataReceived;

		public ItemType AmmoType
		{
			get
			{
				return this._ammoType;
			}
		}

		public IReloadUnloadValidatorModule.Authorization ReloadAuthorization
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

		public IReloadUnloadValidatorModule.Authorization UnloadAuthorization
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

		public int AmmoMax
		{
			get
			{
				return this._defaultCapacity + (int)base.Firearm.AttachmentsValue(AttachmentParam.MagazineCapacityModifier);
			}
		}

		public int AmmoStored
		{
			get
			{
				return this.GetAmmoStoredForSerial(base.ItemSerial);
			}
			private set
			{
				if (!this.MagazineInserted)
				{
					return;
				}
				MagazineModule.SyncData[base.ItemSerial] = value + 1;
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

		public DisplayAmmoValues PredictedDisplayAmmo
		{
			get
			{
				return default(DisplayAmmoValues);
			}
		}

		private Inventory UserInv
		{
			get
			{
				return base.Firearm.OwnerInventory;
			}
		}

		public static bool GetMagazineInserted(ushort serial)
		{
			int num;
			return MagazineModule.SyncData.TryGetValue(serial, out num) && num > 0;
		}

		public void ServerSetInstanceAmmo(ushort serial, int amount)
		{
			MagazineModule.SyncData[serial] = amount + 1;
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteUShort(serial);
				writer.WriteByte((byte)(amount + 1));
			}, true);
		}

		[ExposedFirearmEvent]
		public void ServerRemoveMagazine()
		{
			if (!base.IsServer)
			{
				return;
			}
			this.UserInv.ServerAddAmmo(this.AmmoType, this.AmmoStored);
			this.MagazineInserted = false;
			this.ServerResyncData();
		}

		[ExposedFirearmEvent]
		public void ServerInsertMagazine()
		{
			if (!base.IsServer)
			{
				return;
			}
			this.ServerInsertEmptyMagazine();
			this.ServerLoadAmmoFromInventory();
		}

		[ExposedFirearmEvent]
		public void ServerInsertEmptyMagazine()
		{
			if (!base.IsServer)
			{
				return;
			}
			this.MagazineInserted = true;
			this.ServerResyncData();
		}

		[ExposedFirearmEvent]
		public void ServerLoadAmmoFromInventory(int insertionLimit)
		{
			if (!base.IsServer)
			{
				return;
			}
			int num = this.AmmoMax - this.AmmoStored;
			int num2 = Mathf.Min(Mathf.Min((int)this.UserInv.GetCurAmmo(this.AmmoType), num), insertionLimit);
			this.UserInv.ServerAddAmmo(this.AmmoType, -num2);
			this.AmmoStored += num2;
			this.ServerResyncData();
		}

		[ExposedFirearmEvent]
		public void ServerLoadAmmoFromInventory()
		{
			this.ServerLoadAmmoFromInventory(int.MaxValue);
		}

		public void ServerResyncData()
		{
			int syncData;
			if (!MagazineModule.SyncData.TryGetValue(base.ItemSerial, out syncData))
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteUShort(this.ItemSerial);
				writer.WriteByte((byte)syncData);
			}, true);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			while (reader.Remaining > 0)
			{
				ushort num = reader.ReadUShort();
				byte b = reader.ReadByte();
				MagazineModule.SyncData[num] = (int)b;
				Action<ushort> onDataReceived = MagazineModule.OnDataReceived;
				if (onDataReceived != null)
				{
					onDataReceived(num);
				}
			}
		}

		public void ServerModifyAmmo(int amt)
		{
			this.AmmoStored += amt;
			this.ServerResyncData();
		}

		public int GetAmmoStoredForSerial(ushort serial)
		{
			int num;
			if (!MagazineModule.SyncData.TryGetValue(serial, out num))
			{
				return 0;
			}
			return Mathf.Max(0, num - 1);
		}

		public DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial)
		{
			return new DisplayAmmoValues(this.GetAmmoStoredForSerial(serial), 0);
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
				foreach (KeyValuePair<ushort, int> keyValuePair in MagazineModule.SyncData)
				{
					writer.WriteUShort(keyValuePair.Key);
					writer.WriteByte((byte)keyValuePair.Value);
				}
			});
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this._magazineRemovedOverrideLayers.Update(base.Firearm, !this.MagazineInserted);
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.MagazineAmmo, this.AmmoStored, true);
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsMagInserted, this.MagazineInserted, true);
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (!base.IsServer)
			{
				return;
			}
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

		internal override void OnAttachmentsApplied()
		{
			base.OnAttachmentsApplied();
			if (!base.IsServer)
			{
				return;
			}
			int num = this.AmmoStored - this.AmmoMax;
			if (num <= 0)
			{
				return;
			}
			this.AmmoStored -= num;
			this.UserInv.ServerAddAmmo(this.AmmoType, num);
			this.ServerResyncData();
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			if (!base.IsServer)
			{
				return;
			}
			this.ServerResyncData();
		}

		internal override void ServerProcessMapgenDistribution(ItemPickupBase pickupBase)
		{
			base.ServerProcessMapgenDistribution(pickupBase);
			FirearmPickup firearmPickup = pickupBase as FirearmPickup;
			if (firearmPickup == null)
			{
				return;
			}
			Firearm firearm;
			if (!AttachmentPreview.TryGet(firearmPickup.CurId, false, out firearm))
			{
				return;
			}
			MagazineModule magazineModule;
			if (!firearm.TryGetModule(out magazineModule, true))
			{
				return;
			}
			MagazineModule.SyncData[firearmPickup.CurId.SerialNumber] = magazineModule.AmmoMax + 1;
		}

		internal override void ServerProcessScp914Creation(ushort serial, Scp914KnobSetting knobSetting, Scp914Result scp914Result, ItemType itemType)
		{
			base.ServerProcessScp914Creation(serial, knobSetting, scp914Result, itemType);
			if (itemType != ItemType.ParticleDisruptor)
			{
				return;
			}
			MagazineModule.SyncData[serial] = this.AmmoMax + 1;
			int syncData;
			if (!MagazineModule.SyncData.TryGetValue(serial, out syncData))
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteUShort(serial);
				writer.WriteByte((byte)syncData);
			}, true);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			MagazineModule.SyncData.Clear();
		}

		public bool ValidateAmmoDrop(ItemType id)
		{
			IReloaderModule reloaderModule;
			return id != this.AmmoType || !base.Firearm.TryGetModule(out reloaderModule, true) || !reloaderModule.IsReloadingOrUnloading;
		}

		private static readonly Dictionary<ushort, int> SyncData = new Dictionary<ushort, int>();

		[SerializeField]
		private int _defaultCapacity;

		[SerializeField]
		private ItemType _ammoType;

		[SerializeField]
		private AnimatorConditionalOverride _magazineRemovedOverrideLayers;
	}
}
