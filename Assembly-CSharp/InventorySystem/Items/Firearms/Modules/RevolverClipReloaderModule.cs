using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class RevolverClipReloaderModule : AnimatorReloaderModuleBase
{
	private static readonly Dictionary<ushort, int> SyncWithheld = new Dictionary<ushort, int>();

	private int _serverWithheld;

	private int _clientWithheld;

	private bool _withheldDirty;

	public int WithheldAmmo
	{
		get
		{
			if (!base.IsServer)
			{
				return this._clientWithheld;
			}
			return this._serverWithheld;
		}
	}

	private IPrimaryAmmoContainerModule AmmoContainer
	{
		get
		{
			if (!base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
			{
				throw new InvalidOperationException("The " + base.Firearm.name + " does not have a IPrimaryAmmoContainerModule required by the RevolverClipReloaderModule.");
			}
			return module;
		}
	}

	private int ServerWithheld
	{
		get
		{
			return this._serverWithheld;
		}
		set
		{
			if (value < 0)
			{
				value = 0;
			}
			if (this._serverWithheld != value)
			{
				this._serverWithheld = value;
				this._withheldDirty = true;
				this.OnWithheld?.Invoke();
			}
		}
	}

	public event Action<int> OnAmmoInserted;

	public event Action OnWithheld;

	[ExposedFirearmEvent]
	public void ServerWithholdAmmo()
	{
		if (base.IsServer)
		{
			this.ServerResetWithheldAmmo();
			Inventory ownerInventory = base.Firearm.OwnerInventory;
			ItemType ammoType = this.AmmoContainer.AmmoType;
			this.ServerWithheld = Mathf.Min(ownerInventory.GetCurAmmo(ammoType), this.AmmoContainer.AmmoMax);
			ownerInventory.ServerAddAmmo(ammoType, -this.ServerWithheld);
		}
	}

	[ExposedFirearmEvent]
	public void InsertAmmoFromClip()
	{
		int num = Mathf.Min(this.WithheldAmmo, this.AmmoContainer.AmmoMax);
		if (base.IsServer)
		{
			this.AmmoContainer.ServerModifyAmmo(num);
		}
		this.OnAmmoInserted?.Invoke(num);
		if (base.IsServer)
		{
			this.ServerWithheld -= num;
			this.ServerResetWithheldAmmo();
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsServer && this._withheldDirty)
		{
			this._withheldDirty = false;
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(ReloaderMessageHeader.Custom);
				x.WriteByte((byte)this.ServerWithheld);
			});
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		this.ServerResetWithheldAmmo();
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		RevolverClipReloaderModule.SyncWithheld.Clear();
	}

	protected override void OnInit()
	{
		base.OnInit();
		this.ClientFetchWithheld();
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		this.ClientFetchWithheld();
	}

	protected override MessageInterceptionResult InterceptMessage(NetworkReader reader, ushort serial, ReloaderMessageHeader header, AutosyncMessageType scenario)
	{
		if (header != ReloaderMessageHeader.Custom || (scenario != AutosyncMessageType.RpcTemplate && scenario != AutosyncMessageType.RpcInstance))
		{
			return base.InterceptMessage(reader, serial, header, scenario);
		}
		int num = reader.ReadByte();
		RevolverClipReloaderModule.SyncWithheld[serial] = num;
		if (scenario == AutosyncMessageType.RpcInstance && this._clientWithheld != num)
		{
			this._clientWithheld = num;
			this.OnWithheld?.Invoke();
		}
		return MessageInterceptionResult.Stop;
	}

	protected override void StartReloading()
	{
		this.DecockAndPlayAnim(FirearmAnimatorHashes.Reload);
	}

	protected override void StartUnloading()
	{
		this.DecockAndPlayAnim(FirearmAnimatorHashes.Unload);
	}

	private void ClientFetchWithheld()
	{
		this._clientWithheld = RevolverClipReloaderModule.SyncWithheld.GetValueOrDefault(base.ItemSerial);
	}

	private void ServerResetWithheldAmmo()
	{
		if (base.IsServer && this.ServerWithheld > 0)
		{
			base.Firearm.OwnerInventory.ServerAddAmmo(this.AmmoContainer.AmmoType, this.ServerWithheld);
			this.ServerWithheld = 0;
		}
	}

	private void DecockAndPlayAnim(int hash)
	{
		if (base.Firearm.TryGetModule<DoubleActionModule>(out var module) && module.Cocked)
		{
			module.TriggerDecocking(hash);
		}
		else
		{
			base.Firearm.AnimSetTrigger(hash);
		}
	}
}
