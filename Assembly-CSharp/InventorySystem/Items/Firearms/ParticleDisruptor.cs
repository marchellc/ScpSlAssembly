using System;
using System.Collections.Generic;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Pickups;
using Mirror;

namespace InventorySystem.Items.Firearms
{
	public class ParticleDisruptor : Firearm, IItemAlertDrawer, IItemDrawer, ICustomDescriptionItem
	{
		public CustomDescriptionGui CustomGuiPrefab { get; private set; }

		public override bool AllowHolster
		{
			get
			{
				return base.AllowHolster && (!this._actionModule.IsFiring || this.DeleteOnDrop);
			}
		}

		public override bool AllowDropping
		{
			get
			{
				return true;
			}
		}

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Custom;
			}
		}

		public AlertContent Alert
		{
			get
			{
				return this._hint.Alert;
			}
		}

		public string[] CustomDescriptionContent
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
				return !this._actionModule.IsFiring && this._magazineModule.AmmoStored == 0;
			}
		}

		public override void InitializeSubcomponents()
		{
			base.InitializeSubcomponents();
			this.TryGetModules(out this._actionModule, out this._magazineModule, out this._equipperModule);
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			if (this._equipperModule.IsEquipped && !base.PrimaryActionBlocked)
			{
				this._hint.Update();
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
			if (!base.IsServer)
			{
				return;
			}
			this.ServerSendBrokenRpc(base.ItemSerial);
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
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

		public static readonly HashSet<ushort> BrokenSerials = new HashSet<ushort>();

		private static readonly string[] DescriptionNonAlloc = new string[1];

		private DisruptorActionModule _actionModule;

		private MagazineModule _magazineModule;

		private IEquipperModule _equipperModule;

		private readonly ItemHintAlertHelper _hint = new ItemHintAlertHelper(InventoryGuiTranslation.DisruptorToggleModeHint, new ActionName?(ActionName.WeaponAlt), 0.3f, 0f, 6f);
	}
}
