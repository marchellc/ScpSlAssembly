using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class FlashlightAttachment : SerializableAttachment, ILightEmittingItem
	{
		public static event Action OnAnyStatusChanged;

		public bool IsEmittingLight
		{
			get
			{
				return this.IsEnabled && FlashlightAttachment.GetEnabled(base.ItemSerial);
			}
		}

		public override bool IsEnabled
		{
			get
			{
				return base.IsEnabled;
			}
			set
			{
				if (value == this.IsEnabled)
				{
					return;
				}
				base.IsEnabled = value;
				if (!base.IsServer)
				{
					return;
				}
				if (FlashlightAttachment.GetEnabled(base.ItemSerial))
				{
					return;
				}
				this.ServerSendStatus(true);
			}
		}

		private void ServerSendStatus(bool status)
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				FlashlightAttachment.RpcType rpcType = (status ? FlashlightAttachment.RpcType.Enabled : FlashlightAttachment.RpcType.Disabled);
				writer.WriteByte((byte)rpcType);
			}, true);
		}

		protected override void EnabledEquipUpdate()
		{
			base.EnabledEquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			if (!base.GetActionDown(ActionName.ToggleFlashlight))
			{
				return;
			}
			this.SendCmd(null);
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			FlashlightAttachment.DisabledSerials.Clear();
		}

		internal override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			if (!base.IsServer)
			{
				return;
			}
			this.ServerSendStatus(true);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.IsEnabled || base.ItemUsageBlocked)
			{
				return;
			}
			bool flag = !FlashlightAttachment.GetEnabled(base.ItemSerial);
			PlayerTogglingWeaponFlashlightEventArgs playerTogglingWeaponFlashlightEventArgs = new PlayerTogglingWeaponFlashlightEventArgs(base.Firearm.Owner, base.Firearm, flag);
			PlayerEvents.OnTogglingWeaponFlashlight(playerTogglingWeaponFlashlightEventArgs);
			if (!playerTogglingWeaponFlashlightEventArgs.IsAllowed)
			{
				return;
			}
			flag = playerTogglingWeaponFlashlightEventArgs.NewState;
			this.ServerSendStatus(flag);
			PlayerEvents.OnToggledWeaponFlashlight(new PlayerToggledWeaponFlashlightEventArgs(base.Firearm.Owner, base.Firearm, flag));
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				FlashlightAttachment.DisabledSerials.Remove(serial);
				break;
			case 1:
				FlashlightAttachment.DisabledSerials.Add(serial);
				break;
			case 2:
				FlashlightAttachment.DisabledSerials.Clear();
				while (reader.Remaining >= 2)
				{
					ushort num = reader.ReadUShort();
					FlashlightAttachment.DisabledSerials.Add(num);
				}
				break;
			}
			Action onAnyStatusChanged = FlashlightAttachment.OnAnyStatusChanged;
			if (onAnyStatusChanged == null)
			{
				return;
			}
			onAnyStatusChanged();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
			base.ServerOnPlayerConnected(hub, firstSubcomponent);
			if (!firstSubcomponent)
			{
				return;
			}
			this.SendRpc(hub, delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(FlashlightAttachment.RpcType.FullResync);
				foreach (ushort num in FlashlightAttachment.DisabledSerials)
				{
					writer.WriteUShort(num);
				}
			});
		}

		public static bool GetEnabled(ushort serial)
		{
			return !FlashlightAttachment.DisabledSerials.Contains(serial);
		}

		private static readonly HashSet<ushort> DisabledSerials = new HashSet<ushort>();

		private enum RpcType
		{
			Enabled,
			Disabled,
			FullResync
		}
	}
}
