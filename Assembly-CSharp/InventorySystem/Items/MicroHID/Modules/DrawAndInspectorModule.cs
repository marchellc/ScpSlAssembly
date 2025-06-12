using System;
using System.Collections.Generic;
using AudioPooling;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules;

public class DrawAndInspectorModule : MicroHidModuleBase
{
	private enum RpcType
	{
		AddPickup,
		OnEquipped,
		OnHolstered,
		InspectRequested
	}

	private static readonly HashSet<ushort> PickupAnimSerials = new HashSet<ushort>();

	[SerializeField]
	private AudioClip _equipSound;

	[SerializeField]
	private AudioClip _pickupSound;

	public static event Action<ushort> OnInspectRequested;

	public static bool CheckPickupPreference(ushort serial)
	{
		return DrawAndInspectorModule.PickupAnimSerials.Contains(serial);
	}

	public void ServerRegisterSerial(ushort serial)
	{
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteByte(0);
			x.WriteUShort(serial);
		});
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.OnHolstered:
			DrawAndInspectorModule.PickupAnimSerials.Remove(serial);
			{
				foreach (AudioPoolSession activeSession in AudioManagerModule.GetController(serial).ActiveSessions)
				{
					AudioClip clip = activeSession.Source.clip;
					if (!(clip != this._equipSound) || !(clip != this._pickupSound))
					{
						activeSession.Source.Stop();
					}
				}
				break;
			}
		case RpcType.OnEquipped:
			AudioManagerModule.GetController(serial).PlayOneShot(DrawAndInspectorModule.CheckPickupPreference(serial) ? this._pickupSound : this._equipSound);
			break;
		case RpcType.AddPickup:
			while (reader.Remaining > 0)
			{
				DrawAndInspectorModule.PickupAnimSerials.Add(reader.ReadUShort());
			}
			break;
		case RpcType.InspectRequested:
			DrawAndInspectorModule.OnInspectRequested?.Invoke(serial);
			break;
		}
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(1);
			});
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteByte(2);
		});
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsControllable && base.GetActionDown(ActionName.InspectItem))
		{
			this.SendCmd();
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (!base.ItemUsageBlocked)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteByte(3);
			});
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		DrawAndInspectorModule.PickupAnimSerials.Clear();
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
	{
		base.ServerOnPlayerConnected(hub, firstSubcomponent);
		this.SendRpc(hub, delegate(NetworkWriter x)
		{
			x.WriteByte(0);
			foreach (ushort pickupAnimSerial in DrawAndInspectorModule.PickupAnimSerials)
			{
				x.WriteUShort(pickupAnimSerial);
			}
		});
	}
}
