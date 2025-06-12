using System.Collections.Generic;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms;

public class ParticleDisruptor : Firearm
{
	public static readonly HashSet<ushort> BrokenSerials = new HashSet<ushort>();

	private static readonly string[] DescriptionNonAlloc = new string[1];

	private DisruptorActionModule _actionModule;

	private MagazineModule _magazineModule;

	private IEquipperModule _equipperModule;

	private readonly ItemHintAlertHelper _hint = new ItemHintAlertHelper(InventoryGuiTranslation.DisruptorToggleModeHint, ActionName.WeaponAlt, 0.3f, 0f);

	[SerializeField]
	private CustomDescriptionGui _descriptionPrefab;

	public override CustomDescriptionGui CustomGuiPrefab => this._descriptionPrefab;

	public override bool AllowHolster
	{
		get
		{
			if (base.AllowHolster)
			{
				if (this._actionModule.IsFiring)
				{
					return this.DeleteOnDrop;
				}
				return true;
			}
			return false;
		}
	}

	public override bool AllowDropping => true;

	public override ItemDescriptionType DescriptionType => ItemDescriptionType.Custom;

	public override AlertContent Alert => this._hint.Alert;

	public override string[] CustomDescriptionContent
	{
		get
		{
			ParticleDisruptor.DescriptionNonAlloc[0] = MicroHIDItem.FormatCharge(InventoryGuiTranslation.RemainingAmmo, this._magazineModule.AmmoStored.ToString());
			return ParticleDisruptor.DescriptionNonAlloc;
		}
	}

	private bool DeleteOnDrop
	{
		get
		{
			if (!this._actionModule.IsFiring)
			{
				return this._magazineModule.AmmoStored == 0;
			}
			return false;
		}
	}

	public override void InitializeSubcomponents()
	{
		base.InitializeSubcomponents();
		this.TryGetModules<DisruptorActionModule, MagazineModule, IEquipperModule>(out this._actionModule, out this._magazineModule, out this._equipperModule);
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (NetworkServer.active && !(pickup == null) && pickup.transform.TryGetComponentInParent<Locker>(out var _))
		{
			base.OwnerInventory.ServerSelectItem(base.ItemSerial);
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (this._equipperModule.IsEquipped && !base.PrimaryActionBlocked)
		{
			this._hint.Update(base.Owner);
		}
	}

	public override ItemPickupBase ServerDropItem(bool spawn)
	{
		if (this.DeleteOnDrop)
		{
			this.ServerDestroyItem();
			return null;
		}
		return base.ServerDropItem(spawn);
	}

	public void ServerDestroyItem()
	{
		if (base.IsServer)
		{
			this.ServerSendBrokenRpc(base.ItemSerial);
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}
	}

	public void ServerSendBrokenRpc(ushort serial)
	{
		base.ServerSendPublicRpc(delegate(NetworkWriter x)
		{
			x.WriteByte(byte.MaxValue);
			x.WriteUShort(serial);
		});
	}

	protected override void OnClientReady()
	{
		base.OnClientReady();
		ParticleDisruptor.BrokenSerials.Clear();
	}

	protected override void ClientProcessMainRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessMainRpcTemplate(reader, serial);
		ParticleDisruptor.BrokenSerials.Add(reader.ReadUShort());
	}
}
