using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;

namespace InventorySystem.Items.Firearms.Attachments;

public class FlashlightAttachment : SerializableAttachment, ILightEmittingItem
{
	private enum RpcType
	{
		Enabled,
		Disabled,
		FullResync
	}

	private static readonly HashSet<ushort> DisabledSerials = new HashSet<ushort>();

	public bool IsEmittingLight
	{
		get
		{
			if (this.IsEnabled)
			{
				return FlashlightAttachment.GetEnabled(base.ItemSerial);
			}
			return false;
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
			if (value != this.IsEnabled)
			{
				base.IsEnabled = value;
				if (base.IsServer && !FlashlightAttachment.GetEnabled(base.ItemSerial))
				{
					this.ServerSendStatus(status: true);
				}
			}
		}
	}

	public static event Action OnAnyStatusChanged;

	private void ServerSendStatus(bool status)
	{
		this.SendRpc(delegate(NetworkWriter writer)
		{
			RpcType rpcType = ((!status) ? RpcType.Disabled : RpcType.Enabled);
			writer.WriteByte((byte)rpcType);
		});
	}

	protected override void EnabledEquipUpdate()
	{
		base.EnabledEquipUpdate();
		if (base.IsControllable && base.GetActionDown(ActionName.ToggleFlashlight))
		{
			this.SendCmd();
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		FlashlightAttachment.DisabledSerials.Clear();
	}

	internal override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (base.IsServer)
		{
			this.ServerSendStatus(status: true);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.IsEnabled && !base.ItemUsageBlocked)
		{
			bool newState = !FlashlightAttachment.GetEnabled(base.ItemSerial);
			PlayerTogglingWeaponFlashlightEventArgs e = new PlayerTogglingWeaponFlashlightEventArgs(base.Firearm.Owner, base.Firearm, newState);
			PlayerEvents.OnTogglingWeaponFlashlight(e);
			if (e.IsAllowed)
			{
				newState = e.NewState;
				this.ServerSendStatus(newState);
				PlayerEvents.OnToggledWeaponFlashlight(new PlayerToggledWeaponFlashlightEventArgs(base.Firearm.Owner, base.Firearm, newState));
			}
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.Enabled:
			FlashlightAttachment.DisabledSerials.Remove(serial);
			break;
		case RpcType.Disabled:
			FlashlightAttachment.DisabledSerials.Add(serial);
			break;
		case RpcType.FullResync:
			FlashlightAttachment.DisabledSerials.Clear();
			while (reader.Remaining >= 2)
			{
				ushort item = reader.ReadUShort();
				FlashlightAttachment.DisabledSerials.Add(item);
			}
			break;
		}
		FlashlightAttachment.OnAnyStatusChanged?.Invoke();
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
			writer.WriteSubheader(RpcType.FullResync);
			foreach (ushort disabledSerial in FlashlightAttachment.DisabledSerials)
			{
				writer.WriteUShort(disabledSerial);
			}
		});
	}

	public static bool GetEnabled(ushort serial)
	{
		return !FlashlightAttachment.DisabledSerials.Contains(serial);
	}
}
