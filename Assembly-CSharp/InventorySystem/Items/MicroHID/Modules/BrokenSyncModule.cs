using System;
using System.Collections.Generic;
using AudioPooling;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.MicroHID.Modules;

public class BrokenSyncModule : MicroHidModuleBase
{
	private enum RpcType
	{
		BreakSpecific,
		ResyncAll,
		UnbreakSpecific
	}

	private static readonly Dictionary<ushort, double> BrokenSerialTimes = new Dictionary<ushort, double>();

	[SerializeField]
	private AudioClip _explosionClip;

	[SerializeField]
	private float _explosionSoundRange;

	public bool Broken => BrokenSyncModule.GetBroken(base.ItemSerial);

	public static event Action<ushort> OnBroken;

	public static bool GetBroken(ushort serial)
	{
		return BrokenSyncModule.BrokenSerialTimes.ContainsKey(serial);
	}

	public static bool TryGetBrokenElapsed(ushort serial, out float elapsed)
	{
		if (BrokenSyncModule.BrokenSerialTimes.TryGetValue(serial, out var value))
		{
			elapsed = (float)(NetworkTime.time - value);
			return true;
		}
		elapsed = 0f;
		return false;
	}

	public void ServerSetBroken(ushort serial, bool broken)
	{
		if (broken)
		{
			BrokenSyncModule.BrokenSerialTimes[serial] = NetworkTime.time;
		}
		else
		{
			BrokenSyncModule.BrokenSerialTimes.Remove(serial);
		}
		this.SendRpc(delegate(NetworkWriter writer)
		{
			RpcType rpcType = ((!broken) ? RpcType.UnbreakSpecific : RpcType.BreakSpecific);
			writer.WriteByte((byte)rpcType);
			writer.WriteUShort(serial);
		});
	}

	public void ServerSetBroken()
	{
		if (!base.IsServer)
		{
			throw new InvalidOperationException("Attempting to set broken when NetworkServer is not active.");
		}
		this.ServerSetBroken(base.ItemSerial, broken: true);
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		if (NetworkServer.active)
		{
			return;
		}
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.BreakSpecific:
		{
			serial = reader.ReadUShort();
			bool num = BrokenSyncModule.BrokenSerialTimes.ContainsKey(serial);
			BrokenSyncModule.BrokenSerialTimes[serial] = NetworkTime.time;
			if (!num)
			{
				BrokenSyncModule.OnBroken?.Invoke(serial);
				CycleController cycleController = CycleSyncModule.GetCycleController(serial);
				AudioController controller = AudioManagerModule.GetController(serial);
				MicroHidPhase phase = cycleController.Phase;
				if (phase == MicroHidPhase.WoundUpSustain || phase == MicroHidPhase.WindingDown)
				{
					controller.PlayOneShot(this._explosionClip, this._explosionSoundRange, MixerChannel.Weapons);
				}
			}
			break;
		}
		case RpcType.UnbreakSpecific:
			BrokenSyncModule.BrokenSerialTimes.Remove(reader.ReadUShort());
			break;
		case RpcType.ResyncAll:
			BrokenSyncModule.BrokenSerialTimes.Clear();
			while (reader.Remaining > 0)
			{
				BrokenSyncModule.BrokenSerialTimes.Add(reader.ReadUShort(), 0.0);
			}
			break;
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		BrokenSyncModule.BrokenSerialTimes.Clear();
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.IsServer && this.Broken)
		{
			this.ServerSetBroken();
		}
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
	{
		base.ServerOnPlayerConnected(hub, firstSubcomponent);
		if (firstSubcomponent)
		{
			this.SendRpc(hub, delegate(NetworkWriter writer)
			{
				writer.WriteByte(1);
				BrokenSyncModule.BrokenSerialTimes.ForEachKey(writer.WriteUShort);
			});
		}
	}
}
