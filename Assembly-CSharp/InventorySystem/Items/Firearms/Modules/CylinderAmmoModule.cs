using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class CylinderAmmoModule : ModuleBase, IPrimaryAmmoContainerModule, IAmmoContainerModule, IReloadUnloadValidatorModule, IAmmoDropPreventer
{
	public class Chamber
	{
		private readonly ClientPredictedValue<ChamberState> _predictedState;

		public ChamberState ServerSyncState;

		public ChamberState PredictedState
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

		public ChamberState ContextState
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
				}
				else
				{
					this.PredictedState = value;
				}
			}
		}

		public Chamber()
		{
			this.ServerSyncState = ChamberState.Empty;
			this._predictedState = new ClientPredictedValue<ChamberState>(() => this.ServerSyncState);
		}

		public byte ToByte(int offset)
		{
			return (byte)((uint)this.ServerSyncState << offset * 2);
		}

		public void FromByte(byte b, int offset)
		{
			int num = (b >> offset * 2) & 3;
			this.ServerSyncState = (ChamberState)num;
		}
	}

	public enum ChamberState : byte
	{
		Empty,
		Live,
		Discharged
	}

	private static readonly Dictionary<ushort, Chamber[]> ChambersCache = new Dictionary<ushort, Chamber[]>();

	private static ChamberState[] _rotationBuffer = new ChamberState[16];

	private const int ChambersPerByte = 4;

	private const int BitsPerChamber = 2;

	[SerializeField]
	private int _defaultCapacity;

	[SerializeField]
	private int _ammoInsertionOffset;

	private int? _lastAmmoMax;

	private bool _needsResyncing;

	[field: SerializeField]
	public ItemType AmmoType { get; private set; }

	public int AmmoMax => this._defaultCapacity + (int)base.Firearm.AttachmentsValue(AttachmentParam.MagazineCapacityModifier);

	public int AmmoStored
	{
		get
		{
			int num = 0;
			ReadOnlySpan<Chamber> chambers = this.Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				if (chambers[i].ContextState == ChamberState.Live)
				{
					num++;
				}
			}
			return num;
		}
	}

	public ReadOnlySpan<Chamber> Chambers
	{
		get
		{
			int ammoMax = this.AmmoMax;
			return new ReadOnlySpan<Chamber>(CylinderAmmoModule.GetChambersArrayForSerial(base.ItemSerial, ammoMax), 0, ammoMax);
		}
	}

	public IReloadUnloadValidatorModule.Authorization ReloadAuthorization
	{
		get
		{
			int curAmmo = base.Firearm.OwnerInventory.GetCurAmmo(this.AmmoType);
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

	private bool AllChambersEmpty
	{
		get
		{
			ReadOnlySpan<Chamber> chambers = this.Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				if (chambers[i].ContextState != ChamberState.Empty)
				{
					return false;
				}
			}
			return true;
		}
	}

	public static event Action<ushort> OnChambersModified;

	public bool ValidateAmmoDrop(ItemType id)
	{
		if (id == this.AmmoType && base.Firearm.TryGetModule<IReloaderModule>(out var module))
		{
			return !module.IsReloadingOrUnloading;
		}
		return true;
	}

	public void ServerModifyAmmo(int amt)
	{
		this.ModifyAmmo(amt);
		this._needsResyncing = true;
	}

	public void ModifyAmmo(int amt)
	{
		int num = amt;
		ReadOnlySpan<Chamber> chambers = this.Chambers;
		int length = chambers.Length;
		for (int i = 0; i < length; i++)
		{
			int num2 = i + this._ammoInsertionOffset;
			Chamber chamber = chambers[num2 % length];
			if (num != 0)
			{
				bool flag = chamber.ServerSyncState == ChamberState.Live;
				if (num > 0 && !flag)
				{
					chamber.ServerSyncState = ChamberState.Live;
					num--;
				}
				if (num < 0 && flag)
				{
					chamber.ServerSyncState = ChamberState.Empty;
					num++;
				}
				continue;
			}
			break;
		}
	}

	[ExposedFirearmEvent]
	public void RotateCylinder(int rotations)
	{
		ReadOnlySpan<Chamber> chambers = this.Chambers;
		int length = chambers.Length;
		while (rotations < 0)
		{
			rotations += length;
		}
		if (rotations % length != 0)
		{
			if (CylinderAmmoModule._rotationBuffer.Length < length)
			{
				CylinderAmmoModule._rotationBuffer = new ChamberState[length * 2];
			}
			for (int i = 0; i < length; i++)
			{
				CylinderAmmoModule._rotationBuffer[i] = chambers[i].ContextState;
			}
			for (int j = 0; j < length; j++)
			{
				int num = (j + rotations) % length;
				chambers[j].ContextState = CylinderAmmoModule._rotationBuffer[num];
			}
			this._needsResyncing = true;
		}
	}

	public void ServerResync()
	{
		if (CylinderAmmoModule.ChambersCache.TryGetValue(base.ItemSerial, out var chambers))
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				CylinderAmmoModule.ServerWriteChambers(writer, chambers);
			});
		}
		this._needsResyncing = false;
	}

	public void ClientHoldPrediction()
	{
		ReadOnlySpan<Chamber> chambers = this.Chambers;
		for (int i = 0; i < chambers.Length; i++)
		{
			Chamber obj = chambers[i];
			obj.PredictedState = obj.ContextState;
		}
	}

	[ExposedFirearmEvent]
	public void UnloadAllChambers()
	{
		int ammoStored = this.AmmoStored;
		ReadOnlySpan<Chamber> chambers = this.Chambers;
		for (int i = 0; i < chambers.Length; i++)
		{
			chambers[i].ContextState = ChamberState.Empty;
		}
		if (base.IsServer)
		{
			this._needsResyncing = true;
			base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, ammoStored);
		}
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
				this.ServerModifyAmmo(this.AmmoMax);
				break;
			case ItemAddReason.StartingItem:
				this.ServerModifyAmmo(this.AmmoMax);
				base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, -this.AmmoStored);
				break;
			}
			this._needsResyncing = true;
		}
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
		Chamber[] chambersArrayForSerial = CylinderAmmoModule.GetChambersArrayForSerial(serial, num);
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
			chambersArrayForSerial[j].ServerSyncState = ChamberState.Empty;
		}
		CylinderAmmoModule.OnChambersModified?.Invoke(serial);
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsUnloaded, this.AllChambersEmpty, checkIfExists: true);
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
		this._lastAmmoMax = this.AmmoMax;
		if (!lastAmmoMax.HasValue || lastAmmoMax.Value == this._lastAmmoMax || !CylinderAmmoModule.ChambersCache.TryGetValue(base.ItemSerial, out var value) || value == null)
		{
			return;
		}
		int num = 0;
		foreach (Chamber obj in value)
		{
			if (obj.ContextState == ChamberState.Live)
			{
				num++;
			}
			obj.ContextState = ChamberState.Empty;
		}
		int num2 = num - this._lastAmmoMax.Value;
		if (num2 > 0)
		{
			base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoType, num2);
		}
		this.ServerModifyAmmo(num);
	}

	public int GetAmmoStoredForSerial(ushort serial)
	{
		if (!CylinderAmmoModule.ChambersCache.TryGetValue(serial, out var value) || value == null)
		{
			return 0;
		}
		int num = 0;
		Chamber[] array = value;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].ContextState == ChamberState.Live)
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
		if (pickupBase is FirearmPickup firearmPickup && AttachmentPreview.TryGet(firearmPickup.CurId, reValidate: false, out var result) && result.TryGetModule<CylinderAmmoModule>(out var module))
		{
			CylinderAmmoModule.ServerPrepareNewChambers(firearmPickup.CurId.SerialNumber, module.AmmoMax);
		}
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
	{
		base.ServerOnPlayerConnected(hub, firstModule);
		if (!firstModule)
		{
			return;
		}
		foreach (KeyValuePair<ushort, Chamber[]> data in CylinderAmmoModule.ChambersCache)
		{
			this.SendRpc(hub, delegate(NetworkWriter x)
			{
				x.WriteUShort(data.Key);
				CylinderAmmoModule.ServerWriteChambers(x, data.Value);
			});
		}
	}

	private void LateUpdate()
	{
		if (base.IsServer && this._needsResyncing)
		{
			this.ServerResync();
		}
	}

	public static Chamber[] GetChambersArrayForSerial(ushort serial, int minLength)
	{
		if (!CylinderAmmoModule.ChambersCache.TryGetValue(serial, out var value))
		{
			value = new Chamber[minLength];
			for (int i = 0; i < minLength; i++)
			{
				value[i] = new Chamber();
			}
			CylinderAmmoModule.ChambersCache[serial] = value;
			return value;
		}
		if (value.Length < minLength)
		{
			int num = value.Length;
			Array.Resize(ref value, minLength);
			for (int j = num; j < minLength; j++)
			{
				value[j] = new Chamber();
			}
			CylinderAmmoModule.ChambersCache[serial] = value;
		}
		return value;
	}

	private static void ServerPrepareNewChambers(ushort serial, int ammoLoaded)
	{
		Chamber[] chambersArrayForSerial = CylinderAmmoModule.GetChambersArrayForSerial(serial, ammoLoaded);
		for (int i = 0; i < ammoLoaded; i++)
		{
			chambersArrayForSerial[i].ServerSyncState = ChamberState.Live;
		}
	}

	private static void ServerWriteChambers(NetworkWriter writer, Chamber[] chambers)
	{
		int num = 0;
		for (int num2 = chambers.Length - 1; num2 >= 0; num2--)
		{
			if (chambers[num2].ContextState != ChamberState.Empty)
			{
				num = num2;
				break;
			}
		}
		int num3 = 0;
		byte b = 0;
		for (int i = 0; i <= num; i++)
		{
			int num4 = num3 % 4;
			b |= chambers[i].ToByte(num4);
			if (num4 + 1 == 4)
			{
				writer.WriteByte(b);
				b = 0;
			}
			num3++;
		}
		if (b != 0)
		{
			writer.WriteByte(b);
		}
	}
}
