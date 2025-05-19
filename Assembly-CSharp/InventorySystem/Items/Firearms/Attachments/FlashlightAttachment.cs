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
			if (IsEnabled)
			{
				return GetEnabled(base.ItemSerial);
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
			if (value != IsEnabled)
			{
				base.IsEnabled = value;
				if (base.IsServer && !GetEnabled(base.ItemSerial))
				{
					ServerSendStatus(status: true);
				}
			}
		}
	}

	public static event Action OnAnyStatusChanged;

	private void ServerSendStatus(bool status)
	{
		SendRpc(delegate(NetworkWriter writer)
		{
			RpcType rpcType = ((!status) ? RpcType.Disabled : RpcType.Enabled);
			writer.WriteByte((byte)rpcType);
		});
	}

	protected override void EnabledEquipUpdate()
	{
		base.EnabledEquipUpdate();
		if (base.IsControllable && GetActionDown(ActionName.ToggleFlashlight))
		{
			SendCmd();
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		DisabledSerials.Clear();
	}

	internal override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (base.IsServer)
		{
			ServerSendStatus(status: true);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (IsEnabled && !base.ItemUsageBlocked)
		{
			bool newState = !GetEnabled(base.ItemSerial);
			PlayerTogglingWeaponFlashlightEventArgs playerTogglingWeaponFlashlightEventArgs = new PlayerTogglingWeaponFlashlightEventArgs(base.Firearm.Owner, base.Firearm, newState);
			PlayerEvents.OnTogglingWeaponFlashlight(playerTogglingWeaponFlashlightEventArgs);
			if (playerTogglingWeaponFlashlightEventArgs.IsAllowed)
			{
				newState = playerTogglingWeaponFlashlightEventArgs.NewState;
				ServerSendStatus(newState);
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
			DisabledSerials.Remove(serial);
			break;
		case RpcType.Disabled:
			DisabledSerials.Add(serial);
			break;
		case RpcType.FullResync:
			DisabledSerials.Clear();
			while (reader.Remaining >= 2)
			{
				ushort item = reader.ReadUShort();
				DisabledSerials.Add(item);
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
		SendRpc(hub, delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(RpcType.FullResync);
			foreach (ushort disabledSerial in DisabledSerials)
			{
				writer.WriteUShort(disabledSerial);
			}
		});
	}

	public static bool GetEnabled(ushort serial)
	{
		return !DisabledSerials.Contains(serial);
	}
}
