using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class CylinderAmmoModule : ModuleBase, IPrimaryAmmoContainerModule, IAmmoContainerModule, IReloadUnloadValidatorModule, IAmmoDropPreventer
	{
		public static event Action<ushort> OnChambersModified;

		public ItemType AmmoType { get; private set; }

		public int AmmoMax
		{
			get
			{
				return this._defaultCapacity + (int)base.Firearm.AttachmentsValue(AttachmentParam.MagazineCapacityModifier);
			}
		}

		public unsafe int AmmoStored
		{
			get
			{
				int num = 0;
				ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
				for (int i = 0; i < chambers.Length; i++)
				{
					if (chambers[i]->ContextState == CylinderAmmoModule.ChamberState.Live)
					{
						num++;
					}
				}
				return num;
			}
		}

		public ReadOnlySpan<CylinderAmmoModule.Chamber> Chambers
		{
			get
			{
				int ammoMax = this.AmmoMax;
				return new ReadOnlySpan<CylinderAmmoModule.Chamber>(CylinderAmmoModule.GetChambersArrayForSerial(base.ItemSerial, ammoMax), 0, ammoMax);
			}
		}

		public IReloadUnloadValidatorModule.Authorization ReloadAuthorization
		{
			get
			{
				int curAmmo = (int)base.Firearm.OwnerInventory.GetCurAmmo(this.AmmoType);
				if (this.AmmoStored >= this.AmmoMax || curAmmo <= 0)
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
				if (!this.AllChambersEmpty)
				{
					return IReloadUnloadValidatorModule.Authorization.Allowed;
				}
				return IReloadUnloadValidatorModule.Authorization.Idle;
			}
		}

		private unsafe bool AllChambersEmpty
		{
			get
			{
				ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
				for (int i = 0; i < chambers.Length; i++)
				{
					if (chambers[i]->ContextState != CylinderAmmoModule.ChamberState.Empty)
					{
						return false;
					}
				}
				return true;
			}
		}

		public bool ValidateAmmoDrop(ItemType id)
		{
			IReloaderModule reloaderModule;
			return id != this.AmmoType || !base.Firearm.TryGetModule(out reloaderModule, true) || !reloaderModule.IsReloadingOrUnloading;
		}

		public void ServerModifyAmmo(int amt)
		{
			this.ModifyAmmo(amt);
			this._needsResyncing = true;
		}

		public unsafe void ModifyAmmo(int amt)
		{
			int num = amt;
			ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
			int length = chambers.Length;
			for (int i = 0; i < length; i++)
			{
				int num2 = i + this._ammoInsertionOffset;
				CylinderAmmoModule.Chamber chamber = *chambers[num2 % length];
				if (num == 0)
				{
					break;
				}
				bool flag = chamber.ServerSyncState == CylinderAmmoModule.ChamberState.Live;
				if (num > 0 && !flag)
				{
					chamber.ServerSyncState = CylinderAmmoModule.ChamberState.Live;
					num--;
				}
				if (num < 0 && flag)
				{
					chamber.ServerSyncState = CylinderAmmoModule.ChamberState.Empty;
					num++;
				}
			}
		}

		[ExposedFirearmEvent]
		public unsafe void RotateCylinder(int rotations)
		{
			ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
			int length = chambers.Length;
			while (rotations < 0)
			{
				rotations += length;
			}
			if (rotations % length == 0)
			{
				return;
			}
			if (CylinderAmmoModule._rotationBuffer.Length < length)
			{
				CylinderAmmoModule._rotationBuffer = new CylinderAmmoModule.ChamberState[length * 2];
			}
			for (int i = 0; i < length; i++)
			{
				CylinderAmmoModule._rotationBuffer[i] = chambers[i]->ContextState;
			}
			for (int j = 0; j < length; j++)
			{
				int num = (j + rotations) % length;
				chambers[j]->ContextState = CylinderAmmoModule._rotationBuffer[num];
			}
			this._needsResyncing = true;
		}

		public void ServerResync()
		{
			CylinderAmmoModule.Chamber[] chambers;
			if (CylinderAmmoModule.ChambersCache.TryGetValue(base.ItemSerial, out chambers))
			{
				this.SendRpc(delegate(NetworkWriter writer)
				{
					CylinderAmmoModule.ServerWriteChambers(writer, chambers);
				}, true);
			}
			this._needsResyncing = false;
		}

		public unsafe void ClientHoldPrediction()
		{
			ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				object obj = *chambers[i];
				obj.PredictedState = obj.ContextState;
			}
		}

		[ExposedFirearmEvent]
		public unsafe void UnloadAllChambers()
		{
			int ammoStored = this.AmmoStored;
			ReadOnlySpan<CylinderAmmoModule.Chamber> chambers = this.Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				chambers[i]->ContextState = CylinderAmmoModule.ChamberState.Empty;
			}
			if (!base.IsServer)
			{
				return;
			}
			this._needsResyncing = true;
			base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, ammoStored);
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
				this.ServerModifyAmmo(this.AmmoMax);
				break;
			case ItemAddReason.StartingItem:
				this.ServerModifyAmmo(this.AmmoMax);
				base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, -this.AmmoStored);
				break;
			}
			this._needsResyncing = true;
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			if (NetworkServer.active)
			{
				return;
			}
			if (serial == 0)
			{
				serial = reader.ReadUShort();
			}
			int num = reader.Remaining * 4;
			CylinderAmmoModule.Chamber[] chambersArrayForSerial = CylinderAmmoModule.GetChambersArrayForSerial(serial, num);
			byte b = 0;
			for (int i = 0; i < num; i++)
			{
				int num2 = i % 4;
				if (num2 == 0)
				{
					b = reader.ReadByte();
				}
				chambersArrayForSerial[i].FromByte(b, num2);
			}
			for (int j = num; j < chambersArrayForSerial.Length; j++)
			{
				chambersArrayForSerial[j].ServerSyncState = CylinderAmmoModule.ChamberState.Empty;
			}
			Action<ushort> onChambersModified = CylinderAmmoModule.OnChambersModified;
			if (onChambersModified == null)
			{
				return;
			}
			onChambersModified(serial);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsUnloaded, this.AllChambersEmpty, true);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			CylinderAmmoModule.ChambersCache.Clear();
		}

		internal override void OnAttachmentsApplied()
		{
			base.OnAttachmentsApplied();
			if (!base.IsServer)
			{
				return;
			}
			int? lastAmmoMax = this._lastAmmoMax;
			this._lastAmmoMax = new int?(this.AmmoMax);
			if (lastAmmoMax != null)
			{
				int value = lastAmmoMax.Value;
				int? lastAmmoMax2 = this._lastAmmoMax;
				if (!((value == lastAmmoMax2.GetValueOrDefault()) & (lastAmmoMax2 != null)))
				{
					CylinderAmmoModule.Chamber[] array;
					if (!CylinderAmmoModule.ChambersCache.TryGetValue(base.ItemSerial, out array) || array == null)
					{
						return;
					}
					int num = 0;
					foreach (CylinderAmmoModule.Chamber chamber in array)
					{
						if (chamber.ContextState == CylinderAmmoModule.ChamberState.Live)
						{
							num++;
						}
						chamber.ContextState = CylinderAmmoModule.ChamberState.Empty;
					}
					int num2 = num - this._lastAmmoMax.Value;
					if (num2 > 0)
					{
						base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, num2);
					}
					this.ServerModifyAmmo(num);
					return;
				}
			}
		}

		public int GetAmmoStoredForSerial(ushort serial)
		{
			CylinderAmmoModule.Chamber[] array;
			if (!CylinderAmmoModule.ChambersCache.TryGetValue(serial, out array) || array == null)
			{
				return 0;
			}
			int num = 0;
			CylinderAmmoModule.Chamber[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i].ContextState == CylinderAmmoModule.ChamberState.Live)
				{
					num++;
				}
			}
			return num;
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			this._needsResyncing = true;
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
			CylinderAmmoModule cylinderAmmoModule;
			if (!firearm.TryGetModule(out cylinderAmmoModule, true))
			{
				return;
			}
			CylinderAmmoModule.ServerPrepareNewChambers(firearmPickup.CurId.SerialNumber, cylinderAmmoModule.AmmoMax);
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
		{
			base.ServerOnPlayerConnected(hub, firstModule);
			if (!firstModule)
			{
				return;
			}
			using (Dictionary<ushort, CylinderAmmoModule.Chamber[]>.Enumerator enumerator = CylinderAmmoModule.ChambersCache.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					KeyValuePair<ushort, CylinderAmmoModule.Chamber[]> data = enumerator.Current;
					this.SendRpc(hub, delegate(NetworkWriter x)
					{
						x.WriteUShort(data.Key);
						CylinderAmmoModule.ServerWriteChambers(x, data.Value);
					});
				}
			}
		}

		private void LateUpdate()
		{
			if (!base.IsServer || !this._needsResyncing)
			{
				return;
			}
			this.ServerResync();
		}

		public static CylinderAmmoModule.Chamber[] GetChambersArrayForSerial(ushort serial, int minLength)
		{
			CylinderAmmoModule.Chamber[] array;
			if (!CylinderAmmoModule.ChambersCache.TryGetValue(serial, out array))
			{
				array = new CylinderAmmoModule.Chamber[minLength];
				for (int i = 0; i < minLength; i++)
				{
					array[i] = new CylinderAmmoModule.Chamber();
				}
				CylinderAmmoModule.ChambersCache[serial] = array;
				return array;
			}
			if (array.Length < minLength)
			{
				int num = array.Length;
				Array.Resize<CylinderAmmoModule.Chamber>(ref array, minLength);
				for (int j = num; j < minLength; j++)
				{
					array[j] = new CylinderAmmoModule.Chamber();
				}
				CylinderAmmoModule.ChambersCache[serial] = array;
			}
			return array;
		}

		private static void ServerPrepareNewChambers(ushort serial, int ammoLoaded)
		{
			CylinderAmmoModule.Chamber[] chambersArrayForSerial = CylinderAmmoModule.GetChambersArrayForSerial(serial, ammoLoaded);
			for (int i = 0; i < ammoLoaded; i++)
			{
				chambersArrayForSerial[i].ServerSyncState = CylinderAmmoModule.ChamberState.Live;
			}
		}

		private static void ServerWriteChambers(NetworkWriter writer, CylinderAmmoModule.Chamber[] chambers)
		{
			int num = 0;
			for (int i = chambers.Length - 1; i >= 0; i--)
			{
				if (chambers[i].ContextState != CylinderAmmoModule.ChamberState.Empty)
				{
					num = i;
					break;
				}
			}
			int num2 = 0;
			byte b = 0;
			for (int j = 0; j <= num; j++)
			{
				int num3 = num2 % 4;
				b |= chambers[j].ToByte(num3);
				if (num3 + 1 == 4)
				{
					writer.WriteByte(b);
					b = 0;
				}
				num2++;
			}
			if (b != 0)
			{
				writer.WriteByte(b);
			}
		}

		private static readonly Dictionary<ushort, CylinderAmmoModule.Chamber[]> ChambersCache = new Dictionary<ushort, CylinderAmmoModule.Chamber[]>();

		private static CylinderAmmoModule.ChamberState[] _rotationBuffer = new CylinderAmmoModule.ChamberState[16];

		private const int ChambersPerByte = 4;

		private const int BitsPerChamber = 2;

		[SerializeField]
		private int _defaultCapacity;

		[SerializeField]
		private int _ammoInsertionOffset;

		private int? _lastAmmoMax;

		private bool _needsResyncing;

		public class Chamber
		{
			public CylinderAmmoModule.ChamberState PredictedState
			{
				get
				{
					return this._predictedState.Value;
				}
				set
				{
					this._predictedState.Value = value;
				}
			}

			public CylinderAmmoModule.ChamberState ContextState
			{
				get
				{
					if (!NetworkServer.active)
					{
						return this.PredictedState;
					}
					return this.ServerSyncState;
				}
				set
				{
					if (NetworkServer.active)
					{
						this.ServerSyncState = value;
						return;
					}
					this.PredictedState = value;
				}
			}

			public Chamber()
			{
				this.ServerSyncState = CylinderAmmoModule.ChamberState.Empty;
				this._predictedState = new ClientPredictedValue<CylinderAmmoModule.ChamberState>(() => this.ServerSyncState);
			}

			public byte ToByte(int offset)
			{
				return (byte)(this.ServerSyncState << ((offset * 2) & 31));
			}

			public void FromByte(byte b, int offset)
			{
				int num = (b >> offset * 2) & 3;
				this.ServerSyncState = (CylinderAmmoModule.ChamberState)num;
			}

			private readonly ClientPredictedValue<CylinderAmmoModule.ChamberState> _predictedState;

			public CylinderAmmoModule.ChamberState ServerSyncState;
		}

		public enum ChamberState : byte
		{
			Empty,
			Live,
			Discharged
		}
	}
}
